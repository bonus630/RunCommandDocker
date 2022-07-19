using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace RunCommandDocker
{

    public class BindingCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action<T> RunPath;

        public BindingCommand(Action<T> action)
        {
            this.RunPath = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            RunPath.Invoke((T)parameter);
        }
    }

    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action RunPath;

        public SimpleCommand(Action action)
        {
            this.RunPath = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            RunPath.Invoke();
        }
    }
}
