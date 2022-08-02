namespace BigBoyEngine;

public class StateMachine<T> where T : Node {
    public State<T> State, NextState;
    public T Controller;

    public StateMachine(T controller) {
        Controller = controller;
    }

    public void Run(State<T> next) {
        NextState = next;
    }

    public void Process() {
        if (State != null)
            Run(State.Process());
        if (NextState == State) return;
        State?.Exit();
        State = NextState;
        State.Controller = Controller;
        NextState = null;
        State.Enter();
    }
}