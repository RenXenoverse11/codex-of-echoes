namespace CodexOfEchoes.Core.Words
{
    /// <summary>
    /// Word lookup. An interface because Core cannot read files — the Unity layer
    /// loads enable.txt and injects the result, while tests inject a small fixture.
    /// </summary>
    public interface IWordDictionary
    {
        bool Contains(string word);
    }
}
