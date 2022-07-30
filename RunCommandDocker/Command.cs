using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace RunCommandDocker
{

    public abstract class CommandBase : MarshalByRefObject, INotifyPropertyChanged
    {
        private string name;
        public virtual string Name { get { return name; } set { name = value; OnPropertyChanged("Name"); } }
        protected bool isSelected;
        public virtual bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }
        protected bool isExpanded;
        public virtual bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public abstract class CommandCollectionBase<T> : CommandBase where T : CommandBase
    {

        private ObservableCollection<T> items;

        public virtual ObservableCollection<T> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
                OnPropertyChanged("Items");
            }
        }

        public virtual void Order()
        {
            Items.OrderBy(r => r.Name);
        }

        public int Count
        {
            get
            {
                if (Items == null)
                    return 0;
                else
                    return Items.Count;
            }

        }

   

        public virtual void Add(T item)
        {
            if (Items == null)
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
        public bool Contains(T item)
        {
            return Items.Contains(item);
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
        public object Parent { get; set; }
        public string Path { get; set; }
        public override string ToString() { return Name; }
    }

    public class Module : CommandCollectionBase<Command>
    {
        public Project Parent { get; set; }
        public string FullName { get; set; }
        public override string ToString() { return string.Format("{0}/{1}", Parent.Name, Name); }
    }

    public class Command : CommandCollectionBase<Argument>
    {
        public Module Parent { get; set; }
        private int recursionProtection = 0;
        private object[] argumentsCache;
        public event Action<bool> ArgumentsReady;
        public object[] ArgumentsCache
        {
            get
            {
                return this.argumentsCache;
            }
        }

        internal void PrepareArguments()
        {
            recursionProtection++;
            try
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
                                if (recursionProtection > 100)
                                {
                                    objects[i] = null;
                                    return;
                                }

                                objects[i] = (Items[i].Value as Func<Command, object>).Invoke(this);
                            }
                        }
                        else
                            objects[i] = null;
                    }
                }
                this.argumentsCache = objects;
            }
            catch (StackOverflowException e)
            {
                onArgumentsReady(false);
                System.Windows.Forms.MessageBox.Show("UNBOUNDED RECURSION!");
            }
            onArgumentsReady(true);
        }
        private void onArgumentsReady(bool ready)
        {
            if (ArgumentsReady != null)
                ArgumentsReady(ready);
        }
        public string Method { get; set; }
        public override string ToString() { return string.Format("{0}/{1}/{2}", Parent.Parent.Name, Parent.Name, Method); }

        public event Action<Command> CommandSelectedEvent;

        private object returns;
        public object Returns
        {
            get { return returns; }
            set
            {
                returns = value;
                OnPropertyChanged("Returns");
                OnPropertyChanged("ReturnsType");
            }
        }

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
            recursionProtection = 0;
            if (CommandSelectedEvent != null)
                CommandSelectedEvent(this);
        }
        public override bool IsSelected
        {
            get { return base.IsSelected; }
            set
            {
                base.IsSelected = value;

                onCommandSelected();

            }
        }
        private bool canStop = false;
        public bool CanStop
        {
            get { return canStop; }
            set
            {
                canStop = value;

                OnPropertyChanged("CanStop");

            }
        }
        public bool CheckSelected()
        {
            if (IsSelected)
            {
                OnPropertyChanged("Items");
            }
            return IsSelected;
        }
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
                if (range[i] is Argument)
                {
                    arguments.Name = (range[i] as Argument).Name;
                    arguments.ArgumentType = (range[i] as Argument).ArgumentType;
                    arguments.Value = (range[i] as Argument).Value;
                }
                else
                {

                    arguments.Name = (range[i] as Tuple<string, Type>).Item1;
                    arguments.ArgumentType = (range[i] as Tuple<string, Type>).Item2;
                }
                arguments.Parent = this;
                Items.Add(arguments);

            }

        }
        public override bool Equals(object obj)
        {

            try
            {
                Command c = obj as Command;
                if (c == null)
                    return false;
                if (!c.ToString().Equals(this.ToString()))
                    return false;
                if (!c.returnsType.Equals(this.returnsType))
                    return false;
                if (c.Items != null && this.Items != null)
                {
                    if (!c.Items.Count.Equals(this.Items.Count))
                        return false;
                    for (int i = 0; i < c.Items.Count; i++)
                    {
                        if (!c.Items[i].Equals(this.Items[i]))
                            return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    public class Argument : CommandBase
    {
        private Type argumentType;

        public Command Parent { get; set; }
        public Type ArgumentType
        {
            get { return argumentType; }
            set
            {
                argumentType = value;
                OnPropertyChanged("ArgumentType");
            }
        }
        private object _value;
        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }
        public override bool Equals(object obj)
        {
            try
            {
                Argument argument = obj as Argument;
                if (argument == null)
                    return false;
                return (argument.Name.Equals(this.Name)) && (argument.ArgumentType.Equals(this.ArgumentType));
            }
            catch
            {
                return false;
            }
        }
    }
}