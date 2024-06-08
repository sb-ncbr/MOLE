/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using System.Reactive.Subjects;

    public class CommandDispatcher
    {
        Subject<Tuple<string, object>> commandStream = new Subject<Tuple<string, object>>();

        public IObservable<Tuple<string, object>> CommandStream { get { return commandStream; } }

        Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();

        public void Execute(string command, bool broadcast = false)
        {
            var c = this.commands[command];
            if (c.CanExecute(null))
            {
                c.Execute(null);
                if (broadcast) commandStream.OnNext(Tuple.Create<string, object>(command, null));
            }
        }

        public void Execute<T>(string command, T param, bool broadcast = false)
        {
            var c = this.commands[command];
            if (c.CanExecute(param))
            {
                c.Execute(param);
                if (broadcast) commandStream.OnNext(Tuple.Create<string, object>(command, param));
            }
        }

        public ICommand GetCommand(string name)
        {
            return commands[name];
        }

        public CommandDispatcher AddCommand(string name, ICommand command)
        {
            commands[name] = command;
            return this;
        }
    }
}
