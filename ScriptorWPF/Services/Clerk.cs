using log4net;
using Polly;
using Polly.Retry;
using ScriptorWPF.Models;
using Windows.Services.Store;

namespace ScriptorWPF.Services
{
    public interface IClerk
    {
        Task<Result<bool>> PurchaseLicense();
        Task<Result<bool>> IsSubscriptionActive();
    }
    public class Clerk : IClerk
    {
        private StoreContext _context = null;
        private const string subscriptionStoreId = "scriptor_sub";
        private StoreAppLicense _storeAppLicense = null;
        private StoreProduct _storeProduct = null;
        private readonly ILog _logger;
        private readonly AsyncRetryPolicy _clerkRetryPolicy;

        public Clerk(ILog logger)
        {
            _logger = logger;
            _clerkRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, retryCount, context) => _logger.Error($"Retry for clerk {retryCount} due to {exception.GetType().Name}: {exception.Message}")
            );
        }

        public async Task<Result<bool>> IsSubscriptionActive()
        {
            _logger.Info("Validating subscription.");

            var isActiveLicense = new Result<bool>();
            await _clerkRetryPolicy.ExecuteAsync(async () =>
            {
                if (_context == null)
                {
                    _context = StoreContext.GetDefault();
                }

                if (_storeAppLicense == null)
                {
                    _storeAppLicense = await _context.GetAppLicenseAsync();
                }

                //Check if the customer has the rights to the subscription.
                foreach (var addOnLicense in _storeAppLicense.AddOnLicenses)
                {
                    StoreLicense license = addOnLicense.Value;
                    if (license.SkuStoreId.StartsWith(subscriptionStoreId))
                    {
                        isActiveLicense.Value = license.IsActive;
                        isActiveLicense.Success = true;
                        break;
                    }
                }
            });

            return isActiveLicense;
        }

        public async Task<Result<bool>> PurchaseLicense()
        {
            _logger.Info("Purchasing license.");

            var purchaseResult = new Result<bool>();
            await _clerkRetryPolicy.ExecuteAsync(async () =>
            {
                if (_storeProduct == null)
                {
                    await this.InitializeSubscriptionProductAsync();
                }

                var result = await _storeProduct.RequestPurchaseAsync();

                // Capture the error message for the operation, if any.
                var extendedError = string.Empty;
                if (result.ExtendedError != null)
                {
                    extendedError = result.ExtendedError.Message;
                }

                switch (result.Status)
                {
                    case StorePurchaseStatus.Succeeded:
                        // Show a UI to acknowledge that the customer has purchased your subscription 
                        // and unlock the features of the subscription. 
                        purchaseResult.Value = true;
                        purchaseResult.Success = true;
                        break;
                    case StorePurchaseStatus.NotPurchased:
                        purchaseResult.Value = false;
                        purchaseResult.Success = false;
                        purchaseResult.Message = "The purchase did not complete. The customer may have cancelled the purchase. ExtendedError: " + extendedError;
                        _logger.Warn(purchaseResult.Message);
                        break;
                    case StorePurchaseStatus.ServerError:
                    case StorePurchaseStatus.NetworkError:
                        purchaseResult.Value = false;
                        purchaseResult.Success = false;
                        purchaseResult.Message = "The purchase was unsuccessful due to a server or network error. ExtendedError: " + extendedError;
                        _logger.Error(purchaseResult.Message);
                        break;
                    case StorePurchaseStatus.AlreadyPurchased:
                        purchaseResult.Value = true;
                        purchaseResult.Success = false;
                        purchaseResult.Message = "The customer already owns this subscription. ExtendedError: " + extendedError;
                        _logger.Info(purchaseResult.Message);
                        break;
                }
            });

            return purchaseResult;
        }

        private async Task InitializeSubscriptionProductAsync()
        {
            _logger.Info("Initializing subscription info.");

            await _clerkRetryPolicy.ExecuteAsync(async () =>
            {
                if (_storeProduct != null)
                    return;

                if (_context == null)
                {
                    _context = StoreContext.GetDefault();
                }

                var result = await _context.GetAssociatedStoreProductsAsync(["Durable"]);
                if (result.ExtendedError != null)
                {
                    _logger.Error("Something went wrong while getting the add-ons. ExtendedError:" + result.ExtendedError);
                    return;
                }

               //Look for the product that represents the subscription.
               foreach (var item in result.Products)
                    {
                        var product = item.Value;
                        if (product.StoreId == subscriptionStoreId)
                        {
                            _storeProduct = product;
                            return;
                        }
                    }

                _logger.Warn("The subscription was not found.");
            });
        }
    }
}
