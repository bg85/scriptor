namespace ScriptorWPF.Models
{
    public class Result<T>
    {
        public Result()
        {
            Value = default!;
            Success = true;
            Message = string.Empty;
        }

        public Result(T value)
        {
            Value = value;
            Success = true;
            Message = string.Empty;
        }

        public T Value { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
