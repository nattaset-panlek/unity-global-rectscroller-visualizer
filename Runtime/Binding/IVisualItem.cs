namespace GlobalVisualizer
{
    public interface IVisualItem<T>
    {
        void Bind(T data, VisualItemInfo info);

        void Unbind();
    }
}
