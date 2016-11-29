namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer
    {
        private bool ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("move ") && false)
                ExecuteMoveCommand(message.Remove(0, 5));

            return Module.ExecuteClientCommand(this, message);
        }

        private void ExecuteMoveCommand(string command)
        {
            if (command.StartsWith("set "))
            {
                command = command.Remove(0, 4);

                if (command.StartsWith("updaterate "))
                {
                    command = command.Remove(0, 11).Trim();

                    int updateRate;
                    if (command.StartsWith("normal"))
                    {
                        //MovingUpdateRate = 60;
                        SendServerMessage("Set moving correction updaterate to Normal!");
                    }
                    else if (command.StartsWith("fast"))
                    {
                        //MovingUpdateRate = 30;
                        SendServerMessage("Set moving correction updaterate to Fast!");
                    }
                    else if (int.TryParse(command, out updateRate) && updateRate >= 0 && updateRate <= 100)
                    {
                        //MovingUpdateRate = updateRate;
                        SendServerMessage($"Set moving correction updaterate to {updateRate}!");
                    }
                    else
                        SendServerMessage("Number invalid!");
                }

                else
                    SendServerMessage("Invalid command!");
            }

            else
                SendServerMessage("Invalid command!"); 
        }
    }
}