using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Reflection;
using System.Threading;

namespace RunCommandDocker.MyPopup
{
    public class MyPopup : Popup
    {
        public static readonly DependencyProperty ToReflectProperty = DependencyProperty.Register("ToReflect",
                                                    typeof(object), typeof(MyPopup), new FrameworkPropertyMetadata(null));
        public object ToReflect
        {
            get { return GetValue(ToReflectProperty); }
            set { SetValue(ToReflectProperty, value); }
        }

        public event Action PopupCloseEvent;

        private Reflected reflected;
        private Thread getChildrenThread;

        public MyPopup()
        {
            PopupContent popupContent = new PopupContent();
            popupContent.ReflectedIsExpandedEvent += (reflected) => { GetChildrens(reflected); };
            this.Child = popupContent;
        }
        protected override void OnOpened(EventArgs e)
        {
            if (ToReflect != null && !ToReflect.GetType().IsValueType)
            {
                closeCounter = 0;
                runToClose = false;
                base.OnOpened(e);

                reflected = new Reflected();
                object obj = ToReflect;
                reflected.Value = obj;
                try
                {
                    GetChildrens(reflected);
                    (this.DataContext as ProjectsManager).SelectedCommand.ReflectedProp = reflected;

                }
                catch { }
            }
            else
            {
                RequestClose();
            }

        }
        public void GetChildrens(Reflected parent)
        {

            //For get a item in um generic IEnumerable collection
            //1º lets check if type IsGenericType
            //2º Use the main type and GetInterfaces
            //3º Check which interfaces are implementeds
            //IList,ICollection,IEnumerable,...
            //4º Get the generic type GetGenericArguments()
            //5º Use the implemented interface and generic argument to cast reflected collection
            //6º Go through all items and reflect them
            //Can we put these items in the item property?
            //Is required a checking for enum types, enum types can throw exceptions if yours values cames wrong
            object obj = parent.Value;
            Type mainType;
            
           if (string.IsNullOrEmpty(parent.Name))
                parent.Name = obj.GetType().FullName;

            ObservableCollection<Reflected> Childrens = new ObservableCollection<Reflected>();
            if (parent.Name.Equals("Item"))
            {
                mainType = parent.Parent.Value.GetType();
                Type[] interfaces = mainType.GetInterfaces();
                // Type genericType = mainType.GetGenericTypeDefinition();

                Type _interface = interfaces.FirstOrDefault(r => r.Name.Equals("ICollection") || r.Name.Equals("IList") || r.Name.Equals("IEnumerable"));
                if (_interface == null)
                    return;
                var generics = parent.Parent.Value as dynamic;

                foreach (var generic in generics)
                {
                    bool isValueType = false;
                    Type itemType = generic.GetType();
                    try
                    {
                      
                        isValueType = itemType.IsValueType;
                    }
                    catch { }
                    Reflected item = new Reflected() { Name = itemType.Name, Value = generic, IsValueType = isValueType, Parent = parent };
                    if (!isValueType)
                    {
                        //Here can use recursivity to fill all treeview nodes
                        item.Childrens = new ObservableCollection<Reflected>();
                        item.Childrens.Add(null);
                    }

                    Childrens.Add(item);
                }

               
            }
            else
            {
                mainType = obj.GetType();
                var properties = mainType.GetProperties().OrderBy(p=>p.Name);
                foreach (var property in properties)
               
                {
                    object v = null;
                    bool isValueType = false;
                    try
                    {
                        v = property.GetValue(obj, null);
                        isValueType = v.GetType().IsValueType;
                    }
                    catch { }
                    Reflected item = new Reflected() { Name = property.Name, Value = v, IsValueType = isValueType, Parent = parent };
                    if (!isValueType && v !=null)
                    {
                        //Here can use recursivity to fill all treeview nodes
                        item.Childrens = new ObservableCollection<Reflected>();
                        item.Childrens.Add(null);
                    }

                    Childrens.Add(item);
                }
                if (mainType.IsArray)
                {
                    
                    Reflected item = new Reflected() { Name = "Item", Value = obj, IsValueType = false, Parent = parent };
                    item.Childrens = new ObservableCollection<Reflected>();
                    item.Childrens.Add(null);
                    Childrens.Add(item);
                }


            }
            

                parent.Childrens = Childrens;

        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            Close();
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            closeCounter = 0;
            runToClose = false;
            if (counterThread != null)
                counterThread.Abort();
        }

        int closeCounter = 0;
        int ellapseTime = 100;
        int loops = 10;
        bool runToClose = false;
        Thread counterThread;


        private void Close()
        {
            runToClose = true;
            counterThread = new Thread(new ThreadStart(() =>
            {

                while (runToClose)
                {

                    Thread.Sleep(ellapseTime);
                    closeCounter++;
                    if (closeCounter > loops)
                    {
                        try
                        {
                            if (getChildrenThread != null)
                            {
                                getChildrenThread.Abort();
                                getChildrenThread = null;
                            }
                        }
                        catch
                        {

                        }
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            RequestClose();
                        }));
                    }
                }

            }));
            counterThread.IsBackground = true;
            counterThread.Start();

        }
        private void RequestClose()
        {
            if (PopupCloseEvent != null)
                PopupCloseEvent();
        }
    }

}
