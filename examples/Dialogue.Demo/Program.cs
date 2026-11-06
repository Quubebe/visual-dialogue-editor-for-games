using Dialogue.Core;

var question = new DialogueNode { Text = "Куда я попал?" };
var forest = new DialogueNode { Text = "Вы вошли в лес.", IsEnding = true };
var town = new DialogueNode { Text = "Вы вошли в город.", IsEnding = true };

question.Choices.Add(new DialogueChoice
{
    Text = "В лес",
    TargetNodeId = forest.Id
});

question.Choices.Add(new DialogueChoice
{
    Text = "В город",
    TargetNodeId = town.Id
});

var dialogue = new DialogueDocument
{
    Title = "Развилка",
    StartNodeId = question.Id
};

dialogue.Nodes.AddRange(new[] { question, forest, town });

var session = new DialogueSession(dialogue);
Console.WriteLine(session.GetCurrentNode()?.Text);
foreach (var choice in question.Choices)
{
    Console.WriteLine($"- {choice.Text}");
}
bool moved = session.TryChoose(question.Choices[0].Id);
Console.WriteLine($"Переход выполнен: {moved}");
Console.WriteLine(session.GetCurrentNode()?.Text);

var secondSession = new DialogueSession(dialogue);
bool secondMoved = secondSession.TryChoose(question.Choices[1].Id);
Console.WriteLine($"Вторая сессия: {secondMoved}, {secondSession.GetCurrentNode()?.Text}");
Console.WriteLine($"Первая сессия: {session.GetCurrentNode()?.Text}");
