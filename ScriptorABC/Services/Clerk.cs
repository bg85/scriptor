using log4net;
using System;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace ScriptorABC.Services
{
    public interface IClerk
    {
        Task<bool> PurchaseLicense();
        Task<bool> IsSubscriptionActive();
    }
    public class Clerk : IClerk
    {
        private StoreContext _context = null;
        private const string subscriptionStoreId = "scriptor_sub";
        private StoreAppLicense _storeAppLicense = null;
        private StoreProduct _storeProduct = null;
        private ILog _logger;

        public Clerk(ILog logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsSubscriptionActive()
        {
            try
            {
                if (_context == null)
                {
                    _context = StoreContext.GetDefault();
                }

                if (_storeAppLicense == null)
                {
                    _storeAppLicense = await _context.GetAppLicenseAsync();
                }

                // Check if the customer has the rights to the subscription.
                foreach (var addOnLicense in _storeAppLicense.AddOnLicenses)
                {
                    StoreLicense license = addOnLicense.Value;
                    if (license.SkuStoreId.StartsWith(subscriptionStoreId))
                    {
                        if (license.IsActive)
                        {
                            // The expiration date is available in the license.ExpirationDate property.
                            return true;
                        }
                    }
                }

                // The customer does not have a license to the subscription.
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Error validating subscription information", ex);
            }

            return false;
        }

        public async Task<bool> PurchaseLicense()
        {
            // Request a purchase of the subscription product. If a trial is available it will be offered 
            // to the customer. Otherwise, the non-trial SKU will be offered.
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
                    return true;
                case StorePurchaseStatus.NotPurchased:
                    _logger.Error("The purchase did not complete. The customer may have cancelled the purchase. ExtendedError: " + extendedError);
                    break;
                case StorePurchaseStatus.ServerError:
                case StorePurchaseStatus.NetworkError:
                    _logger.Error("The purchase was unsuccessful due to a server or network error. ExtendedError: " + extendedError);
                    break;
                case StorePurchaseStatus.AlreadyPurchased:
                    _logger.Error("The customer already owns this subscription. ExtendedError: " + extendedError);
                    break;
            }

            return false;
        }

        private async Task InitializeSubscriptionProductAsync()
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

            // Look for the product that represents the subscription.
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
        }
    }
}
