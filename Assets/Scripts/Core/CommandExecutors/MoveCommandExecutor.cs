using System.Threading;
using Abstractions.Commands;
using Abstractions.Commands.CommandsInterfaces;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.AI;
using Utils;

namespace Core.CommandExecutors
{
    public class MoveCommandExecutor : CommandExecutorBase<IMoveCommand>
    {
        [SerializeField] private UnitMovementStop _stop;
        [SerializeField] private Animator _animator;
        [SerializeField] private StopCommandExecutor _stopCommandExecutor;
        private ReactiveCollection<IMoveCommand> _moveCommands = new ReactiveCollection<IMoveCommand>();
        private bool _route = false;
        private IMoveCommand _imoveCommand;
        private static readonly int Walk = Animator.StringToHash("Walk");
        private static readonly int Idle = Animator.StringToHash("Idle");

        private void Start()
        {
            _stop.OnStop += OnStop;
        }
        public void OnStop()
        {
            if(_moveCommands.Count > 0)
            {
                _moveCommands.Remove(_moveCommands[_moveCommands.Count - 1]);
                if (_moveCommands.Count < 1)
                {
                    _route = false;
                }
                ExecuteSpecificCommand(_imoveCommand);
            }
        }
        public override async void ExecuteSpecificCommand(IMoveCommand command)
        {
            _imoveCommand = command;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                _moveCommands.Add(command);
                if(!_route)
                {
                    _route = true;
                }
            }
            else 
            {
                if (_moveCommands.Count>0)
                {
                    GetComponent<NavMeshAgent>().destination = _moveCommands[0].Target;
                    for(int i=0; i< _moveCommands.Count; i++)
                    {
                        if(i>0)
                        {
                            _moveCommands.Move(i, i - 1);
                        }
                    }
                }
                else 
                {
                    GetComponent<NavMeshAgent>().destination = command.Target;
                }
            }
            _animator.SetTrigger(Walk);
            _stopCommandExecutor.CancellationTokenSource = new CancellationTokenSource();
            try
            {
                await _stop.WithCancellation  ( _stopCommandExecutor.CancellationTokenSource.Token);
            }
            catch
            {
                GetComponent<NavMeshAgent>().isStopped = true;
                GetComponent<NavMeshAgent>().ResetPath();
            }
            _stopCommandExecutor.CancellationTokenSource = null;
            _animator.SetTrigger(Idle);
        }
    }
}