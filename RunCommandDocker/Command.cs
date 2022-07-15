using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RunCommandDocker
{

    public abstract class CommandBase : MarshalByRefObject, INotifyPropertyChanged
    {
        private string name;
        public virtual string Name { get { return name; } set { name = value; OnPropertyChanged("Name"); } }
        private bool isSelected;
        public virtual bool IsSelected { get { return isSelected; } set { isSelected = value; OnPropertyChanged("IsSelected"); } }
        private bool isExpanded;
        public virtual bool IsExpanded { get { return isExpanded; } set { isExpanded = value; OnPropertyChanged("IsExpanded"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        

    }

    public abstract class CommandCollectionBase<T> : CommandBase
    {

        private ObservableCollection<T> items;

        public virtual ObservableCollection<T> Items {
            get {
                return items;
            }
            set {
                items = value;
                OnPropertyChanged("Items"); } }

        public virtual void Add(T item)
        {
            if(Items==null)
                Items = new ObservableCollection<T>();
            Items.Add(item);
        }
        public virtual void AddRange(T[] range)
        {
            if (Items == null)
                Items = new ObservableCollection<T>();
            for (int i = 0; i < range.Length; i++)
            {
                Items.Add(range[i]);
            }
        }
      
        public virtual void Remove(T item)
        {
            if (Items != null)
                Items.Remove(item);
        }
        public virtual T this[int index] { set { Items[index] = value; } get { return Items[index]; } }
    }

    public class Project : CommandCollectionBase<Module>
    {
        public string Path { get; set; }
    }

    public class Module : CommandCollectionBase<Command>
    {
        public Project Parent { get; set; }
        public string FullName { get; set; }
    }

    public class Command : CommandCollectionBase<Arguments>
    {
        public Module Parent { get; set; }
        public object[] Arguments{get{
                object[] objects = null;
                
                if (this.Items!=null)
                {
                    int length = this.Items.Count;
                    if(length>0)
                        objects = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        objects[i] = Items[i].Value;
                    }
                }


                return objects;
            } }
        public string Method { get; set; }
        public override string ToString() { return string.Format("{0}/{1}/{2}", Parent.Parent.Name, Parent.Name, Method);  }

        public event Action<Command> CommandSelectedEvent;

        private object returns;
        public object Returns { get { return returns; } set { returns = value; OnPropertyChanged("Returns"); OnPropertyChanged("ReturnsType"); } }

        private Type returnsType;
        public Type ReturnsType { get { return returnsType; } set { returnsType = value; OnPropertyChanged("ReturnsType"); } }
        private void onCommandSelected()
        {
            if (CommandSelectedEvent != null)
                CommandSelectedEvent(this);
        }
        public override bool IsSelected { 
            get { return base.IsSelected; } 
            set { base.IsSelected = value;
                onCommandSelected(); } }
        public void AddRange(object[] range)
        {
            if (Items == null)
                Items = new ObservableCollection<Arguments>();
            for (int i = 0; i < range.Length; i++)
            {
                Arguments arguments = new Arguments();

                arguments.Name = (range[i] as Tuple<string, Type>).Item1;
                arguments.ArgumentType = (range[i] as Tuple<string, Type>).Item2;
                Items.Add(arguments);

            }
        }
    }
    public class Arguments : CommandBase
    {
        private Type argumentType;

        public Command Parent { get; set; }
        public Type ArgumentType { get { return argumentType; } set { argumentType = value;OnPropertyChanged("ArgumentType"); } }
        public object Value { get; set; }

    }
}