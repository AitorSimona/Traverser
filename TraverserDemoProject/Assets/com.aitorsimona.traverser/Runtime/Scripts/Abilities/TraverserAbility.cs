namespace Traverser
{
    // --- All abilities derive from this base interface ---
    public interface TraverserAbility
    {
        // --- Called every frame by AbilityController ---
        TraverserAbility OnUpdate(float deltaTime);

        // --- Called every physics step by AbilityController ---
        TraverserAbility OnFixedUpdate(float deltaTime);

        // --- Called from another ability to indicate that a collision is about to happen ---
        // --- Other abilities may take control ---
        bool OnContact(ref TraverserTransform contactTransform, float deltaTime);

        // --- Called from another ability to indicate that the controller is about to lose its ground ---
        // --- Other abilities may take control ---
        bool OnDrop(float deltaTime);

        // --- Called when an ability takes control ---
        void OnEnter();

        // --- Called when an ability loses control ---
        void OnExit();

        // --- Simple method to check if an ability is enabled ---
        bool IsAbilityEnabled();
    }

    // -------------------------------------------------
}

