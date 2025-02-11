
namespace ChatAPI.Extensions;

public class ModelStateValidationException : Exception
{
    public List<string> Errors { get; }

    public ModelStateValidationException(List<string> errors)
    {
        Errors = errors;
    }
}