using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RunCommandDocker
{
    public abstract class CommandBase : INotifyPropertyChanged
    {
        private string name;
        public virtual string Name { get { return name; } set { name = value;OnPropertyChanged("Name"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));  
        }

    }
    public abstract class CommandCollectionBase<T> : CommandBase
    {
        private ObservableCollection<T> items;
        public virtual ObservableCollection<T> Items { get { return items; } set { items = value; OnPropertyChanged("Items"); } }
    }
    public class Project : CommandCollectionBase<Module>
    {
        public string Path { get; set; }
    }
    public class Module:CommandCollectionBase<Command>
    {
        public Project Parent { get; set; }
    }

    public class Command : CommandBase
    {
        public Module Parent { get; set; }
        public string Method { get; set; }
    }
}