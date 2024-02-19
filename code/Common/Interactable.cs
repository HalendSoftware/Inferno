using Sandbox;

public abstract class Interactable : Component
{
    // Message displayed when looking at interactable
    [Property] public string PromptMessage;

    // Message displayed while interacting. Uses PromptMessage if empty.
    [Property] public string InteractingMessage;

    // Time it takes to interact with interactable, 0 for single press interactions
    [Property] public float TimeToInteract;
    [Property] public bool FieldBased;
    [Sync] public bool IsActive { get; set; }

    public abstract void Interact(GameObject player);

    /// <summary>
    /// Determines if the player can interact with this object, hiding the prompt if false.
    /// </summary>
    public virtual bool CanInteract(GameObject player) => true;
}