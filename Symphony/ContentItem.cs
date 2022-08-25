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

    /// <summary>
    /// Called when the content of this item is updated.
    /// </summary>
    protected abstract void OnContentUpdated(object newContent);

    /// <summary>
    /// Called when all content has been loaded. Should perform OpenGL initialization if any is required
    /// </summary>
    public abstract void OnAllContentLoaded();
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