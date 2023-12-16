namespace IctBaden.Stonehenge.Extension
{
    public interface IStateProvider
    {
        bool GetExpanded(string id);
        void SetExpanded(string id, bool expanded);
        
        bool GetChecked(string id);
        void SetChecked(string id, bool expanded);

    }
}