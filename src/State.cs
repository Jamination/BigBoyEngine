namespace BigBoyEngine;

public abstract class State<T> where T : Node {
    public T Controller;

    public abstract void Enter();
    public abstract void Exit();
    public abstract State<T> Process();
}