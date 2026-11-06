using System;

namespace Dialogue.Core;

public class DialogueCharacter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}
