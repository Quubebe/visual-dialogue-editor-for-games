using System;
using Dialogue.Core;

namespace Dialogue.Persistence;

public class DialogueProject
{
    public int ProjectFormatVersion { get; set; } = 1;
    public DialogueDocument Dialogue { get; set; } = new();
    public List<NodePosition> NodePositions { get; set; } = new();

}
