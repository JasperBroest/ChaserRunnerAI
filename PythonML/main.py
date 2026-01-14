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
    def __init__(self, in_features=99, hidden_layer_1_size=256, hidden_layer_2_size=128, out_features=4):   # 4 out features because it outputs probabilities not raw x or y values
        super().__init__() 
        self.fc1 = nn.Linear(in_features, hidden_layer_1_size)
        self.fc2 = nn.Linear(hidden_layer_1_size, hidden_layer_2_size)
        self.out = nn.Linear(hidden_layer_2_size, out_features)

        self.relu = nn.ReLU()
        self.softmax = nn.Softmax(dim=-1)


    def forward(self, x):
        x = self.relu(self.fc1(x))
        x = self.relu(self.fc2(x))  # Activation function,  Process in second layer
        x = self.out(x)  # result = actions
        x = self.softmax(x)  # get the probabilities per action
        return x
    
class Critic(nn.Module):
    def __init__(self, in_features=99, hidden_layer_1_size=128, hidden_layer_2_size=256, out_features=1):
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

actor_optimizer = optim.Adam(actor.parameters(), lr=0.001) # Verantwoordelijk voor het bijstellen van de weights (hoe sterk etc.)
critic_optimizer = optim.Adam(critic.parameters(), lr=0.001) # Verantwoordelijk voor het bijstellen van de weights (hoe sterk etc.)

# Passes a state through actor and returns a discrete action based on the actor's probabilities
def select_action(state):
    state_tensor = torch.from_numpy(state).float().unsqueeze(0)
    action_prob = actor(state_tensor)            
    action_dist = Categorical(action_prob)
    
    action = action_dist.sample()
    log_prob = action_dist.log_prob(action)
    
    return action.item(), log_prob

# This is a non-blocking call that only loads the environment.
build_path = "C:\\Users\\japse\\Unity Projects\\ChaserRunnerAI\\ChaserRunnerAI\\Builds\\ChaserRunnerAI.exe"
engine_config_channel = EngineConfigurationChannel()
time_scale = 5
engine_config_channel.set_configuration_parameters(600, 600, 1, time_scale, -1)
env = UnityEnvironment(file_name=build_path, seed=1, side_channels=[engine_config_channel])

# Start interacting with the environment.
env.reset()
behavior_name = list(env.behavior_specs.keys())[0] 

if __name__ == "__main__":

    for episode in range(10000):
        env.reset()
        done = False
        total_reward = 0

        log_probs = []

        while not done:
            decision_steps, terminal_steps = env.get_steps(behavior_name)   # Info about agents that need action and that finished their episode
            agent_id = list(decision_steps.agent_id)[0] # Select 1st and only agent
            obs = decision_steps.obs[0][0]  # Select observations from first agent
            #print(obs)
            
            action, log_prob = select_action(obs)
            log_probs.append(log_prob)

            # Send action to Unity
            action_tuple = ActionTuple(discrete=np.array([[action]]))
            env.set_action_for_agent(behavior_name, agent_id, action_tuple)
            env.step()

            # Get reward and next_obs
            next_decision_steps, next_terminal_steps = env.get_steps(behavior_name)
            if agent_id in next_terminal_steps: # if agent's episode finised
                done = True
                reward = next_terminal_steps[agent_id].reward
                
                next_obs = np.zeros_like(obs)   # optional placeholder
                state_tensor = torch.from_numpy(obs).float().unsqueeze(0)
                V_current = critic(state_tensor)    # V(s)
                
                V_next = 0.0    # V(s')

                discount_factor = 0.99
                target = reward + discount_factor * V_next
                td_error = target - V_current

                # Convert TD target to tensor
                target_tensor = torch.tensor([target], dtype=torch.float32)

                # Compute critic loss
                critic_loss = (V_current - target_tensor).pow(2)

                # Backpropagate and update critic
                critic_optimizer.zero_grad()
                critic_loss.backward()
                critic_optimizer.step()

                log_prob = log_probs[-1]  # get the log-prob of the last action
                actor_loss = -log_prob * td_error.detach().squeeze()    # detach to prevent backprop through critic

                # Backpropagate and update actor
                actor_optimizer.zero_grad()
                actor_loss.backward()
                actor_optimizer.step()

            else:
                reward = next_decision_steps[agent_id].reward
                # print("We took action " + str(action) + " and got reward " + str(reward))
                next_obs = next_decision_steps.obs[0][0]
            total_reward += reward

        print(f"Episode {episode} done, total reward={round(total_reward, 2)}")
    
    env.close()