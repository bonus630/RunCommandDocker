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
            popupContent.ReflectedIsExpandedEvent += (reflected) => { GetChildrens(reflected.Value, reflected); };
            this.Child = popupContent;
        }
        protected override void OnOpened(EventArgs e)
        {
            if (ToReflect != null)
            {
                closeCounter = 0;
                runToClose = false;
                base.OnOpened(e);
               
                reflected = new Reflected();
                object obj = ToReflect;
               
                try
                {
                    //getChildrenThread = new Thread(new ParameterizedThreadStart((obj) =>
                    //{
                    //    this.Dispatcher.Invoke(new Action(() =>
                    //    {
                            GetChildrens(obj, reflected);
                           
                    //    }));

                    //}));
                    //getChildrenThread.IsBackground = true;
                    //getChildrenThread.Start(ToReflect);
                    (this.DataContext as Command).ReflectedProp = reflected;

                }
                catch { }
            }
            else
            {
                RequestClose();
            }

        }
        public void GetChildrens(object obj, Reflected parent)
        {
            if(!string.IsNullOrEmpty(parent.Name))
                parent.Name = obj.GetType().FullName;
            ObservableCollection<Reflected> Childrens = new ObservableCollection<Reflected>();

            PropertyInfo[] properties = obj.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                object v = null;
                bool isValueType = false;
                try
                {
                    v = properties[i].GetValue(obj, null);
                    isValueType = v.GetType().IsValueType;
                }
                catch { }
                Reflected item = new Reflected() { Name = properties[i].Name, Value = v, IsValueType = isValueType };
                if (!isValueType)
                {
                    //Here can use recursivity to fill all treeview nodes
                    item.Childrens = new ObservableCollection<Reflected>();
                    item.Childrens.Add(null);
                    //GetChildrens(v, item);
                }

                Childrens.Add(item);
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
            counterThread = new Thread(new ThreadStart(() => { 
            
                while(runToClose)
                {
                    
                    Thread.Sleep(ellapseTime);
                    closeCounter++;
                    if(closeCounter > loops)
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
