from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.environment import ActionTuple
import numpy as np
# This is a non-blocking call that only loads the environment.
build_path = "C:\\Users\\japse\\Unity Projects\\ChaserRunnerAI\\ChaserRunnerAI\\Builds\\ChaserRunnerAI.exe"
env = UnityEnvironment(file_name=build_path, seed=1, side_channels=[])

# Start interacting with the environment.
env.reset()
behavior_name = list(env.behavior_specs.keys())[0] 

if __name__ == "__main__":
    for i in range(1000):
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        for j, obs in enumerate(decision_steps.obs):
            print(f"Obs {j}: shape={obs.shape}")
            print(obs[0])  # first agent’s obs

        actions = np.array([[np.random.uniform(-0.5, 0.5), np.random.uniform(-0.5, 0.5)]], dtype=np.float32)
        action_tuple = ActionTuple(continuous=actions, discrete=None)
        env.set_action_for_agent(behavior_name, agent_id=0, action=action_tuple)
        env.step()
    
    env.close()