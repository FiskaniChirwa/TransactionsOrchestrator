namespace Document.Core.Exceptions;

public class DocumentValidationException : Exception
{
    public List<string> ValidationErrors { get; }
    
    public DocumentValidationException(string message) 
        : base(message)
    {
        ValidationErrors = [message];
    }
    
    public DocumentValidationException(List<string> errors) 
        : base($"Document validation failed: {string.Join(", ", errors)}")
    {
        ValidationErrors = errors;
    }
}