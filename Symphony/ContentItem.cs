namespace Symphony;

public abstract class ContentItem
{
    public string Identifier { get; private set; }
    public IContentSource Source { get; private set; }
    public object Content { get; private set; }

    public ContentItem(string identifier, IContentSource source, object content)
    {
        Identifier = identifier;
        Source = source;
        Content = content;
    }

    internal void UpdateContent(IContentSource source, object content)
    {
        this.OnContentUpdated(content);
        Content = content;
        Source = source;
    }

    protected abstract void OnContentUpdated(object newContent);
}

// Can be used for content items, however, it is recommended that you inherit from this class instead and provid
public abstract class ContentItem<T> : ContentItem
{
    public ContentItem(string identifier, IContentSource source, T content)
        : base(identifier, source, content!)
    {
    }

    public new T? Content
    {
        get { return (T)base.Content; }
    }
}