namespace ScriptorABC.Models
{
    public class Result<T>
    {
        public T Value { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }


        public Result() 
        { 
            Value = default; 
            Success = false;
            Message = string.Empty;
        }

        public Result(T value)
        {
            Value = value;
            Success = true;
            Message = string.Empty;
        }
    }
}
