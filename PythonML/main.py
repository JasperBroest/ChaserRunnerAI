import random
from mlagents_envs.environment import UnityEnvironment
import time
# This is a non-blocking call that only loads the environment.
build_path = "C:\\Users\\japse\\Unity Projects\\ChaserRunnerAI\\ChaserRunnerAI\\Builds\\ChaserRunnerAI.exe"
env = UnityEnvironment(file_name=build_path, seed=1, side_channels=[])
# Start interacting with the environment.
env.reset()
behavior_names = env.behavior_specs.keys()

if __name__ == "__main__":
    for i in range(1_000_000):
        env.step()
        env.set_action_for_agent(behavior_name="DefaultBehaviour", agent_id=0, action=[1.0])
        time.sleep(0.5)
    print("hallo")