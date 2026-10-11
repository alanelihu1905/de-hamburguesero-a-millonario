namespace Tycoon3D.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact();
    }

    /// <summary>Acción secundaria opcional (tecla Q), por ejemplo cambiar la apuesta del casino.</summary>
    public interface ISecondaryInteractable
    {
        void InteractSecondary();
    }
}

