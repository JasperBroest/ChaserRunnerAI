from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.environment import ActionTuple
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
import numpy as np
from collections import deque

import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.distributions import Categorical

class Actor(nn.Module):
    def __init__(self, in_features=33, hidden_layer_1_size=256, hidden_layer_2_size=128, out_features=8):
        super().__init__() 
        self.fc1 = nn.Linear(in_features, hidden_layer_1_size)
        self.fc2 = nn.Linear(hidden_layer_1_size, hidden_layer_2_size)
        self.out = nn.Linear(hidden_layer_2_size, out_features)
        self.softmax = nn.Softmax()
        self.relu = nn.ReLU()


    def forward(self, x):
        x = self.fc1(x)
        x = self.fc2(self.relu(x))  ## Activation function,  Process in second layer
        x = self.out(self.relu(x))  # result = actions
        x = self.softmax(x)  # get the probabilities per action
        return x
    
class Critic(nn.Module):
    def __init__(self, in_features=33, hidden_layer_1_size=128, hidden_layer_2_size=256, out_features=1):
        super().__init__() 
        self.fc1 = nn.Linear(in_features, hidden_layer_1_size)
        self.fc2 = nn.Linear(hidden_layer_1_size, hidden_layer_2_size)
        self.out = nn.Linear(hidden_layer_2_size, out_features)
        self.relu = nn.ReLU()

    def forward(self, x):
        x = self.fc1(x)
        x = self.relu(x)  # Activation function
        x = self.fc2(x)  # Process in second layer
        x = self.relu(x)  # Activation function
        x = self.out(x)  # result = actions
        return x

# Instance of Model class
actor = Actor()
critic = Critic()
actor_optimizer = optim.Adam(actor.parameters(), lr=0.0001) # Verantwoordelijk voor het bijstellen van de weights (hoe sterk etc.)
critic_optimizer = optim.Adam(actor.parameters(), lr=0.0001) # Verantwoordelijk voor het bijstellen van de weights (hoe sterk etc.)

def select_action(state):
    state_tensor = torch.from_numpy(state).float().unsqueeze(0)  # (1,1)
    with torch.no_grad():
        action_prob = actor(state_tensor)
        action_dist= Categorical(action_prob)

        # Sample the action
        action = action_dist.sample()
    return action.detach().cpu().numpy()[0]

# This is a non-blocking call that only loads the environment.
build_path = "C:\\Users\\japse\\Unity Projects\\ChaserRunnerAI\\ChaserRunnerAI\\Builds\\ChaserRunnerAI.exe"
engine_config_channel = EngineConfigurationChannel()
time_scale = 10
engine_config_channel.set_configuration_parameters(600, 600, 1, time_scale, -1)
env = UnityEnvironment(file_name=build_path, seed=1, side_channels=[engine_config_channel])

# Start interacting with the environment.
env.reset()
behavior_name = list(env.behavior_specs.keys())[0] 

if __name__ == "__main__":

    for episode in range(5000):
        env.reset()
        done = False
        total_reward = 0

        log_probs = []

        while not done:
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            agent_id = list(decision_steps.agent_id)[0]
            obs = decision_steps.obs[0][0]  # shape (1,)
            
            # Get action from NN
            action = select_action(obs)

            # Send action to Unity
            action_tuple = ActionTuple(discrete=action.reshape(1,8))
            env.set_action_for_agent(behavior_name, agent_id, action_tuple)
            env.step()

            # Get reward and next_obs
            next_decision_steps, next_terminal_steps = env.get_steps(behavior_name)
            if agent_id in next_terminal_steps:
                reward = next_terminal_steps[agent_id].reward
                
                next_obs = np.zeros_like(obs)
                done = True
            else:
                reward = next_decision_steps[agent_id].reward
                next_obs = next_decision_steps.obs[0][0]
            total_reward += reward

        print(f"Episode {episode} done, total reward={total_reward}")
    
    env.close()