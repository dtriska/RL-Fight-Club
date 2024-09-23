Setup:

Reference: https://unity-technologies.github.io/ml-agents/Installation/#clone-the-ml-agents-toolkit-repository-recommended

Ensure you have Unity 2023.2/Python 3.10.11

Clone the ML-Agent Repo:
git clone --branch release_21 https://github.com/Unity-Technologies/ml-agents.git

Install local com.unity.ml-agent and com.unity.ml-agent-env package in the Unity Editor

Create a virtual environment with Python 3.10.11
python -m venv venv
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

Download the dep. from ml-agent cloned repo
```
cd /path/to/ml-agents
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents
```
