from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.environment import ActionTuple
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
import numpy as np
from collections import deque

import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim

class Model(nn.Module):
    def __init__(self, in_features=33, hidden_layer_1_size=8, hidden_layer_2_size=8, out_features=2):
        super().__init__() 
        self.fc1 = nn.Linear(in_features, hidden_layer_1_size)
        self.fc2 = nn.Linear(hidden_layer_1_size, hidden_layer_2_size)
        self.out = nn.Linear(hidden_layer_2_size, out_features)


    def forward(self, x):
        x = self.fc1(x)
        x = F.relu(x)  # Activation function
        x = self.fc2(x)  # Process in second layer
        x = F.relu(x)  # Activation function
        x = self.out(x)  # result = actions
        x = torch.tanh(x)
        return x

# Instance of Model class
model = Model()
optimizer = optim.Adam(model.parameters(), lr=0.01) # Verantwoordelijk voor het bijstellen van de weights (hoe sterk etc.)

class ReplayBuffer:
    def __init__(self, max_size=5000):
        self.buffer = deque(maxlen=max_size)

    def add(self, obs, action, reward, next_obs, done):
        self.buffer.append((np.array(obs, dtype=np.float32),
                            np.array(action, dtype=np.float32),
                            float(reward),
                            np.array(next_obs, dtype=np.float32),
                            float(done)))

    def sample_all(self):
        obs, actions, rewards, next_obs, dones = zip(*self.buffer)
        return (np.array(obs), np.array(actions), np.array(rewards),
                np.array(next_obs), np.array(dones))

    def clear(self):
        self.buffer.clear()

    def __len__(self):
        return len(self.buffer)


def select_action(state):
    state_tensor = torch.from_numpy(state).float().unsqueeze(0)  # (1,1)
    with torch.no_grad():
        mean_action = model(state_tensor)
    # For simplicity, we just return mean as deterministic action
    return mean_action.detach().cpu().numpy()[0]

# This is a non-blocking call that only loads the environment.
build_path = "C:\\Users\\japse\\Unity Projects\\ChaserRunnerAI\\ChaserRunnerAI\\Builds\\ChaserRunnerAI.exe"
engine_config_channel = EngineConfigurationChannel()
time_scale = 5
engine_config_channel.set_configuration_parameters(600, 600, 1, time_scale, -1)
env = UnityEnvironment(file_name=build_path, seed=1, side_channels=[engine_config_channel])

#Start interacting with the environment.
env.reset()
behavior_name = list(env.behavior_specs.keys())[0] 

if __name__ == "__main__":
    # Initialize buffer
    buffer = ReplayBuffer(max_size=5000)

    for episode in range(1000):
        env.reset()
        done = False
        total_reward = 0
        while not done:
            # Get obs from unity agent
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            agent_id = list(decision_steps.agent_id)[0]
            obs = decision_steps.obs[0][0]  # shape (1,)

            # Get action from NN
            action = select_action(obs)

            # Send action to Unity
            action_tuple = ActionTuple(continuous=action.reshape(1,2))

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

            # Store experience
            buffer.add(obs, action, reward, next_obs, done)

        # Training step after each episode
        if len(buffer) > 0:
            obs_batch, actions_batch, rewards_batch, _, _ = buffer.sample_all()
            obs_tensor = torch.from_numpy(obs_batch).float()
            actions_tensor = torch.from_numpy(actions_batch).float() # Wat heb ik daadwerkelijk uitgevoerd
            rewards_tensor = torch.from_numpy(rewards_batch).float()

            optimizer.zero_grad()
            pred_actions = model(obs_tensor)  # Als ik alles nog eens zou doen (met dezelfde observations!), dan zou ik deze actions doen.
            loss = F.mse_loss(pred_actions, actions_tensor, reduction='none')  # vergelijk deze met elkaar
            loss = (loss.sum(dim=1) * rewards_tensor).mean()  # weight by reward!
            loss.backward()
            optimizer.step()

            buffer.clear()

        print(f"Episode {episode} done, total reward={total_reward}")
    
    env.close()