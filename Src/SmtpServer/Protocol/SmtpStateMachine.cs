﻿using SmtpServer.Text;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SmtpServer.Protocol
{
    internal class SmtpStateMachine
    {
        private delegate bool TryMakeDelegate(TokenEnumerator enumerator, out SmtpCommand command, out SmtpResponse errorResponse);

        private readonly SmtpSessionContext _context;
        private readonly StateTable _stateTable;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The SMTP server session context.</param>
        internal SmtpStateMachine(SmtpSessionContext context)
        {
            _context = context;
            _stateTable = new StateTable
            {
                new State(SmtpState.Initialized)
                {
                    { NoopCommand.Command, TryMakeNoop },
                    { RsetCommand.Command, TryMakeRset },
                    { QuitCommand.Command, TryMakeQuit },
                    { ProxyCommand.Command, TryMakeProxy },
                    { HeloCommand.Command, TryMakeHelo, SmtpState.WaitingForMail },
                    { EhloCommand.Command, TryMakeEhlo, SmtpState.WaitingForMail },
                },
                new State(SmtpState.WaitingForMail)
                {
                    { NoopCommand.Command, TryMakeNoop },
                    { RsetCommand.Command, TryMakeRset },
                    { QuitCommand.Command, TryMakeQuit },
                    { HeloCommand.Command, TryMakeHelo, SmtpState.WaitingForMail },
                    { EhloCommand.Command, TryMakeEhlo, SmtpState.WaitingForMail },
                    { MailCommand.Command, TryMakeMail, SmtpState.WithinTransaction }
                },
                new State(SmtpState.WithinTransaction)
                {
                    { NoopCommand.Command, TryMakeNoop },
                    { RsetCommand.Command, TryMakeRset, SmtpState.WaitingForMail },
                    { QuitCommand.Command, TryMakeQuit },
                    { RcptCommand.Command, TryMakeRcpt, SmtpState.CanAcceptData },
                },
                new State(SmtpState.CanAcceptData)
                {
                    { NoopCommand.Command, TryMakeNoop },
                    { QuitCommand.Command, TryMakeQuit },
                    { RcptCommand.Command, TryMakeRcpt },
                    { DataCommand.Command, TryMakeData, SmtpState.WaitingForMail },
                }
            };

            _stateTable.Initialize(SmtpState.Initialized);
        }

        /// <summary>
        /// Make a delegate to return a known response.
        /// </summary>
        /// <param name="errorResponse">The error response to return.</param>
        /// <returns>The delegate that will return the correct error response.</returns>
        private static TryMakeDelegate MakeResponse(SmtpResponse errorResponse)
        {
            return (TokenEnumerator enumerator, out SmtpCommand command, out SmtpResponse response) =>
            {
                command = null;
                response = errorResponse;

                return false;
            };
        }

        /// <summary>
        /// Advances the enumerator to the next command in the stream.
        /// </summary>
        /// <param name="context">The SMTP session context to allow for session based state transitions.</param>
        /// <param name="tokenEnumerator">The token enumerator to accept the command from.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that indicates why a command could not be accepted.</param>
        /// <returns>true if a valid command was found, false if not.</returns>
        public bool TryMake(SmtpSessionContext context, TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return _stateTable.TryMake(context, tokenEnumerator, out command, out errorResponse);
        }

        /// <summary>
        /// Accept the state and transition to the new state.
        /// </summary>
        /// <param name="context">The session context to use for accepting session based transitions.</param>
        public void Transition(SmtpSessionContext context)
        {
            _stateTable.Transition(context);
        }

        /// <summary>
        /// Try to make a HELO command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a HELO command was found, false if not.</returns>
        private bool TryMakeHelo(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeHelo(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a EHLO command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a EHLO command was found, false if not.</returns>
        private bool TryMakeEhlo(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeEhlo(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a NOOP command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a NOOP command was found, false if not.</returns>
        private bool TryMakeNoop(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeNoop(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a QUIT command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a QUIT command was found, false if not.</returns>
        private bool TryMakeQuit(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeQuit(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a RSET command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a RSET command was found, false if not.</returns>
        private bool TryMakeRset(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeRset(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a STARTTLS command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a STARTTLS command was found, false if not.</returns>
        private bool TryMakeStartTls(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeStartTls(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a MAIL command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a MAIL command was found, false if not.</returns>
        private bool TryMakeMail(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeMail(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a RCPT command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a RCPT command was found, false if not.</returns>
        private bool TryMakeRcpt(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeRcpt(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a Proxy Protocol PROXY header.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a PROXY command was found, false if not.</returns>
        private bool TryMakeProxy(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeProxy(out command, out errorResponse);
        }

        /// <summary>
        /// Try to make a DATA command.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator to use when matching the command.</param>
        /// <param name="command">The command that was found.</param>
        /// <param name="errorResponse">The error response that was returned if a command could not be matched.</param>
        /// <returns>true if a DATA command was found, false if not.</returns>
        private bool TryMakeData(TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
        {
            return new SmtpParser(_context.ServerOptions, tokenEnumerator).TryMakeData(out command, out errorResponse);
        }

        /// <summary>
        /// Returns the waiting for mail state.
        /// </summary>
        State WaitingForMail => _stateTable[SmtpState.WaitingForMail];

        #region StateTable

        private class StateTable : IEnumerable
        {
            private readonly Dictionary<SmtpState, State> _states = new Dictionary<SmtpState, State>();
            private SmtpState _current;
            private StateTransition _transition;

            /// <summary>
            /// Sets the initial state.
            /// </summary>
            /// <param name="stateId">The ID of the initial state.</param>
            public void Initialize(SmtpState stateId)
            {
                _current = stateId;
            }

            /// <summary>
            /// Returns the state with the given ID.
            /// </summary>
            /// <param name="stateId">The state ID to return.</param>
            /// <returns>The state with the given id.</returns>
            public State this[SmtpState stateId] => _states[stateId];

            /// <summary>
            /// Add the given state.
            /// </summary>
            /// <param name="state"></param>
            public void Add(State state)
            {
                _states.Add(state.StateId, state);
            }

            /// <summary>
            /// Advances the enumerator to the next command in the stream.
            /// </summary>
            /// <param name="context">The session context to use for making session based transitions.</param>
            /// <param name="tokenEnumerator">The token enumerator to accept the command from.</param>
            /// <param name="command">The command that is defined within the token enumerator.</param>
            /// <param name="errorResponse">The error that indicates why the command could not be made.</param>
            /// <returns>true if a valid command was found, false if not.</returns>
            public bool TryMake(SmtpSessionContext context, TokenEnumerator tokenEnumerator, out SmtpCommand command, out SmtpResponse errorResponse)
            {
                if (_states[_current].Transitions.TryGetValue(tokenEnumerator.Peek().Text, out _transition) == false)
                {
                    var response = $"expected {String.Join("/", _states[_current].Transitions.Keys)}";

                    command = null;
                    errorResponse = new SmtpResponse(SmtpReplyCode.SyntaxError, response);

                    return false;
                }

                if (_transition.Delegate(tokenEnumerator, out command, out errorResponse) == false)
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Accept the state and transition to the new state.
            /// </summary>
            /// <param name="context">The session context to use for accepting session based transitions.</param>
            public void Transition(SmtpSessionContext context)
            {
                _current = _transition.Transition(context);
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                // this is just here for the collection initializer syntax to work
                throw new NotImplementedException();
            }
        }

        #endregion StateTable

        #region State

        private class State : IEnumerable
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="stateId">The ID of the state.</param>
            public State(SmtpState stateId)
            {
                StateId = stateId;
                Transitions = new Dictionary<string, StateTransition>(StringComparer.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Add a state action.
            /// </summary>
            /// <param name="command">The name of the SMTP command.</param>
            /// <param name="tryMake">The function callback to create the command.</param>
            public void Add(string command, TryMakeDelegate tryMake)
            {
                Add(command, tryMake, context => StateId);
            }

            /// <summary>
            /// Add a state action.
            /// </summary>
            /// <param name="command">The name of the SMTP command.</param>
            /// <param name="tryMake">The function callback to create the command.</param>
            /// <param name="transition">The state to transition to.</param>
            public void Add(string command, TryMakeDelegate tryMake, SmtpState transition)
            {
                Add(command, tryMake, context => transition);
            }

            /// <summary>
            /// Add a state action.
            /// </summary>
            /// <param name="command">The name of the SMTP command.</param>
            /// <param name="tryMake">The function callback to create the command.</param>
            /// <param name="transition">The function to determine the new state.</param>
            public void Add(string command, TryMakeDelegate tryMake, Func<SmtpSessionContext, SmtpState> transition)
            {
                Transitions.Add(command, new StateTransition(tryMake, transition));
            }

            /// <summary>
            /// Add a state action.
            /// </summary>
            /// <param name="command">The name of the SMTP command.</param>
            /// <param name="tryMake">The function callback to create the command.</param>
            /// <param name="transitionTo">The state to transition to.</param>
            public void Replace(string command, TryMakeDelegate tryMake, SmtpState? transitionTo = null)
            {
                Remove(command);
                Add(command, tryMake, transitionTo ?? StateId);
            }

            /// <summary>
            /// Clear the command from the current state.
            /// </summary>
            /// <param name="command">The command to clear.</param>
            public void Remove(string command)
            {
                Transitions.Remove(command);
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                // this is just here for the collection initializer syntax to work
                throw new NotImplementedException();
            }

            /// <summary>
            /// Gets ID of the state.
            /// </summary>
            public SmtpState StateId { get; }

            /// <summary>
            /// Gets the actions that are available to the state.
            /// </summary>
            public Dictionary<string, StateTransition> Transitions { get; }
        }

        #endregion State

        #region StateTransition

        private class StateTransition
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="delegate">The delegate to match the input.</param>
            /// <param name="transition">The transition function to move from the previous state to the new state.</param>
            public StateTransition(TryMakeDelegate @delegate, Func<SmtpSessionContext, SmtpState> transition)
            {
                Delegate = @delegate;
                Transition = transition;
            }

            /// <summary>
            /// The delegate to match the input.
            /// </summary>
            public TryMakeDelegate Delegate { get; }

            /// <summary>
            /// The transition function to move from the previous state to the new state.
            /// </summary>
            public Func<SmtpSessionContext, SmtpState> Transition { get; }
        }

        #endregion StateTransition
    }
}