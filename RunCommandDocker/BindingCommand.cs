using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace RunCommandDocker
{
    public class BindingCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action<Command> RunPath;

        public BindingCommand(Action<Command> action)
        {
            this.RunPath = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            RunPath.Invoke((Command)parameter);
        }
    }
}
