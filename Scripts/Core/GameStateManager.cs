// GameStateManager is responsible for the application flow.
using Godot;
using System;

// GameState defines the state of the game, menu, location, or interface the user is in.
public enum GameState
{
    Initializing, // displaying game logo splash, and starting autostart scripts
    MainMenu, // displaying play modes
    SoloPlay, // displaying the singleplayer interfaces
    MultiPlay, // displaying the  multiplayer interfaces
    Playing, // currently inside a game session
    Finalizing, // currently ending a game session
    Exiting // about to exit the game

}

class InvalidStateTransition : Exception
{
    public InvalidStateTransition(GameState CurrentState, GameState NextState) : base($"[!] InvalidStateTransition: Cannot change {CurrentState} to {NextState}.") { }
}

public partial class GameStateManager : Node
{
    [Export]
    public GameState CurrentState { get; private set; } = GameState.Initializing;
    public GameState LastState { get; private set; } = GameState.Initializing;
    
    [Signal]
    public delegate void StateChangedEventHandler(GameState NewState);

    
    public override void _Ready()
    {
        StateChanged += Log;
        TransitionTo(GameState.MainMenu);
        TransitionTo(GameState.SoloPlay);
        TransitionTo(GameState.Playing);
        TransitionTo(GameState.Finalizing);
        TransitionTo(GameState.Exiting);
    }

    public void TransitionTo(GameState NextState)
    {
        if (NextState == CurrentState) return;
        if (!isValidTransition(CurrentState, NextState)) {GD.PushError(new InvalidStateTransition(CurrentState, NextState).Message); return;}
        LastState = CurrentState;
        CurrentState = NextState;
        EmitSignal(SignalName.StateChanged, Variant.From(NextState));
    }
    private bool isValidTransition(GameState CurrentState, GameState NextState)
    {
        return CurrentState switch
        {
            GameState.Initializing => NextState == GameState.MainMenu,
            
            GameState.MainMenu     => NextState == GameState.SoloPlay || 
                                      NextState == GameState.MultiPlay || 
                                      NextState == GameState.Exiting,
            
            GameState.SoloPlay     => NextState == GameState.Playing || 
                                      NextState == GameState.MainMenu || 
                                      NextState == GameState.Exiting,
            
            GameState.MultiPlay    => NextState == GameState.Playing || 
                                      NextState == GameState.MainMenu || 
                                      NextState == GameState.Exiting,
            
            GameState.Playing      => NextState == GameState.Finalizing || 
                                      NextState == GameState.Exiting,
            
            GameState.Finalizing   => NextState == GameState.MainMenu || 
                                      NextState == GameState.MultiPlay || 
                                      NextState == GameState.SoloPlay || 
                                      NextState == GameState.Exiting,
            
            _ => false
        };
    }

    // temporary debugging call
    public void Log(GameState NextState)
    {
        GD.Print($"[i] State changed from {LastState} to {NextState}.");
    }

}
