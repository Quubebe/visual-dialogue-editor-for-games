using System;

namespace Dialogue.Core;

public class DialogueValidationIssue
{
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public Guid? NodeId { get; set; }
}
