public interface IUIPanel
{
    void Init();
    void Show(bool hideAni, bool force);
    void Hide(bool hideAni, bool force, bool init);
}
