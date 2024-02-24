namespace AmiaReforged.Core.CommandQuery.Types;

public interface IResult<T> where T : class
{
    public T Value { get; set; }
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
}