using System.ComponentModel;

namespace LogWindowUI
{
    public class LogSystem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        private bool m_enabled;
        public bool Enabled
        {
            get
            {
                return m_enabled;
            }

            set
            {
                m_enabled = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Enabled"));
                }
            }
        }
    }
}
