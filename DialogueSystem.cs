using System;
using System.Collections.Generic;
using System.Linq;

public class DialogueOption
{
    public string Text { get; set; }
    public int NextNodeId { get; set; }
    public Action OnSelect { get; set; }

    public DialogueOption(string text, int nextNodeId, Action onSelect = null)
    {
        Text = text;
        NextNodeId = nextNodeId;
        OnSelect = onSelect;
    }
}

public class DialogueNode
{
    public int Id { get; set; }
    public string Text { get; set; }
    public List<DialogueOption> Options { get; set; }

    public DialogueNode(int id, string text)
    {
        Id = id;
        Text = text;
        Options = new List<DialogueOption>();
    }
}

public class Dialogue
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Dictionary<int, DialogueNode> Nodes { get; set; }

    public Dialogue(int id, string name)
    {
        Id = id;
        Name = name;
        Nodes = new Dictionary<int, DialogueNode>();
    }
}

public class DialogueSystem
{
    private Dictionary<int, Dialogue> dialogues;
    private int nextDialogueId;

    public DialogueSystem()
    {
        dialogues = new Dictionary<int, Dialogue>();
        nextDialogueId = 1;
    }

    public int CreateDialogue(string name)
    {
        int id = nextDialogueId++;
        dialogues[id] = new Dialogue(id, name);
        return id;
    }

    public void AddDialogueNode(int dialogueId, int nodeId, string text)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue))
        {
            dialogue.Nodes[nodeId] = new DialogueNode(nodeId, text);
        }
        else
        {
            throw new ArgumentException($"Dialogue with ID {dialogueId} not found.");
        }
    }

    public void AddDialogueOption(int dialogueId, int nodeId, string optionText, int nextNodeId, Action onSelect = null)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue) && dialogue.Nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            node.Options.Add(new DialogueOption(optionText, nextNodeId, onSelect));
        }
        else
        {
            throw new ArgumentException($"Dialogue or node not found. DialogueID: {dialogueId}, NodeID: {nodeId}");
        }
    }

    public void RemoveDialogueOption(int dialogueId, int nodeId, int optionIndex)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue) && dialogue.Nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            if (optionIndex >= 0 && optionIndex < node.Options.Count)
            {
                node.Options.RemoveAt(optionIndex);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), "Option index is out of range.");
            }
        }
        else
        {
            throw new ArgumentException($"Dialogue or node not found. DialogueID: {dialogueId}, NodeID: {nodeId}");
        }
    }

    public void UpdateDialogueNodeText(int dialogueId, int nodeId, string newText)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue) && dialogue.Nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            node.Text = newText;
        }
        else
        {
            throw new ArgumentException($"Dialogue or node not found. DialogueID: {dialogueId}, NodeID: {nodeId}");
        }
    }

    public void UpdateDialogueOptionText(int dialogueId, int nodeId, int optionIndex, string newText)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue) && dialogue.Nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            if (optionIndex >= 0 && optionIndex < node.Options.Count)
            {
                node.Options[optionIndex].Text = newText;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), "Option index is out of range.");
            }
        }
        else
        {
            throw new ArgumentException($"Dialogue or node not found. DialogueID: {dialogueId}, NodeID: {nodeId}");
        }
    }

    public DialogueNode GetDialogueNode(int dialogueId, int nodeId)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue) && dialogue.Nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            return node;
        }
        throw new ArgumentException($"Dialogue or node not found. DialogueID: {dialogueId}, NodeID: {nodeId}");
    }

    public void RemoveDialogue(int dialogueId)
    {
        if (!dialogues.Remove(dialogueId))
        {
            throw new ArgumentException($"Dialogue with ID {dialogueId} not found.");
        }
    }

    public int CloneDialogue(int sourceDialogueId)
    {
        if (dialogues.TryGetValue(sourceDialogueId, out Dialogue sourceDialogue))
        {
            int newDialogueId = CreateDialogue(sourceDialogue.Name + " (Clone)");
            Dialogue newDialogue = dialogues[newDialogueId];

            foreach (var node in sourceDialogue.Nodes)
            {
                newDialogue.Nodes[node.Key] = new DialogueNode(node.Value.Id, node.Value.Text);
                foreach (var option in node.Value.Options)
                {
                    newDialogue.Nodes[node.Key].Options.Add(new DialogueOption(option.Text, option.NextNodeId, option.OnSelect));
                }
            }

            return newDialogueId;
        }
        throw new ArgumentException($"Source dialogue with ID {sourceDialogueId} not found.");
    }

    public Dialogue GetDialogue(int dialogueId)
    {
        if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue))
        {
            return dialogue;
        }
        throw new ArgumentException($"Dialogue with ID {dialogueId} not found.");
    }

    public List<int> GetAllDialogueIds()
    {
        return new List<int>(dialogues.Keys);
    }

    public void StartDialogue(int dialogueId, int startNodeId, Action<DialogueNode> displayCallback, Action<int> selectCallback)
    {
        DialogueNode currentNode = GetDialogueNode(dialogueId, startNodeId);
        displayCallback(currentNode);

        while (true)
        {
            int selection = selectCallback(currentNode.Options.Count);
            if (selection < 0 || selection >= currentNode.Options.Count)
            {
                break;
            }

            DialogueOption selectedOption = currentNode.Options[selection];
            selectedOption.OnSelect?.Invoke();

            if (selectedOption.NextNodeId == -1)
            {
                break;
            }

            currentNode = GetDialogueNode(dialogueId, selectedOption.NextNodeId);
            displayCallback(currentNode);
        }
    }

    public void SaveDialogues(string filePath)
    {
        // Implement serialization logic to save dialogues to a file
        // You can use JSON serialization or any other method you prefer
    }

    public void LoadDialogues(string filePath)
    {
        // Implement deserialization logic to load dialogues from a file
        // You can use JSON deserialization or any other method you prefer
    }
}