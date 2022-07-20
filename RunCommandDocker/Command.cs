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
        public virtual bool IsExpanded { get { return isExpanded; } 
            set { 
                isExpanded = value; 
                OnPropertyChanged("IsExpanded"); } }

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

    

        public int Count
        {
            get { if (Items == null)
                    return 0;
                else
                    return Items.Count; }
            
        }


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

    public class Command : CommandCollectionBase<Argument>
    {
        public Module Parent { get; set; }
        private object[] argumentsCache;
        public object[] ArgumentsCache{get{
                return this.argumentsCache;
            } }

        internal void PrepareArguments()
        {
            object[] objects = null;

            if (this.Items != null)
            {
                int length = this.Items.Count;
                if (length > 0)
                    objects = new object[length];
                for (int i = 0; i < length; i++)
                {
                    if (Items[i].Value != null)
                    {
                        if (Items[i].Value.GetType().IsValueType || Items[i].Value is string)
                        {
                            objects[i] = Convert.ChangeType(Items[i].Value, Items[i].ArgumentType);
                        }
                        else
                        {

                            objects[i] = (Items[i].Value as Func<Command, object>).Invoke(this);

                        }
                    }
                    else
                        objects[i] = null;
                }
            }
            this.argumentsCache = objects;
        }

        public string Method { get; set; }
        public override string ToString() { return string.Format("{0}/{1}/{2}", Parent.Parent.Name, Parent.Name, Method);  }

        public event Action<Command> CommandSelectedEvent;

        private object returns;
        public object Returns { get { return returns; } set { 
                returns = value; 
                OnPropertyChanged("Returns"); 
                OnPropertyChanged("ReturnsType"); } }

        private Type returnsType;
        public Type ReturnsType { get { return returnsType; } set { returnsType = value; OnPropertyChanged("ReturnsType"); } }
        private Reflected reflected;

        public Reflected ReflectedProp
        {
            get { return reflected; }
            set
            {
                reflected = value;
                OnPropertyChanged("ReflectedProp");
            }
        }

        private bool hasParam = false;
        public bool HasParam { get { return hasParam; } set { hasParam = value; OnPropertyChanged("HasParam"); } }
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
            if (Items == null && range.Length > 0)
            {
                Items = new ObservableCollection<Argument>();
                HasParam = true;
            }
            for (int i = 0; i < range.Length; i++)
            {
                Argument arguments = new Argument();

                arguments.Name = (range[i] as Tuple<string, Type>).Item1;
                arguments.ArgumentType = (range[i] as Tuple<string, Type>).Item2;
                arguments.Parent = this;
                Items.Add(arguments);

            }

        }
    }
    public class Argument : CommandBase
    {
        private Type argumentType;

        public Command Parent { get; set; }
        public Type ArgumentType { get { return argumentType; } set { 
                argumentType = value;
                OnPropertyChanged("ArgumentType"); } }
        private object _value;
        public object Value { get { return _value; } set { 
                _value = value;
                OnPropertyChanged("Value"); } }

    }
}