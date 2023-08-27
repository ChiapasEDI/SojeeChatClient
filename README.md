# SojeeChatClient
Copyright 2023, Chiapas EDI Technologies, Inc.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This is Sojee, Chiapas EDI Technologies, Inc. AI front-end chatbot.  It relies on the text-generation-webui as an AI inference engine in order to answer questions.

These instructions are geared toward the following development environment:
- Windows 10/11
- Visual Studio (Community edition is fine) configured for ASP.NET Core development
- Docker w/ WSL2

First, we're going to modify the instructions at [https://github.com/oobabooga/text-generation-webui ](https://github.com/oobabooga/text-generation-webui#alternative-docker)https://github.com/oobabooga/text-generation-webui#alternative-docker to install oobabooga's AI inference engine using a specific model.  Make sure you have Docker Desktop running before you start!

1. From the command line, enter 'wsl' to enter the WSL Ubuntu prompt.
2. Change directory to your workspace
3. git clone https://github.com/oobabooga/text-generation-webui
4. cd text-generation-webui
5. ln -s docker/{Dockerfile,docker-compose.yml,.dockerignore} .
6. cp docker/.env.example .env
7. nano docker-compose.yml - change the 7.5 on line 8 to 8.6
8. nano .env - change the 7.5 on line 4 to 8.6, then replace line 7 to be like this:
   CLI_ARGS=--model TheBloke_OpenAssistant-Llama2-13B-Orca-8K-3319-GPTQ_gptq-8bit-64g-actorder_True --api --triton --listen --auto-devices
9. docker compose up --build
    (wait about 10 minutes if you're on a fast PC - it's going to build the Docker image, start up text-generation-webui)
10. Open up a local web browser to http://localhost:7860, go to the Model tab, go to the window that says "Download custom model or LorA", type in TheBloke/OpenAssistant-Llama2-13B-Orca-8K-3319-GPTQ:gptq-8bit-64g-actorder_True, then click Download.  Wait about 10-15 minutes for the download to complete.
11. Press Control-C in the WSL window a few times, then:
12. docker compose down
13. docker compose up --build

The WSL window should say "Loading TheBloke_OpenAssistant...actorder_True..."   This means you'll have to wait another five minutes for the model to load, and then http://0.0.0.0:5000/api should pop up.  This means the back-end inference engine is now ready for queries.

14. Next, go into Visual Studio and clone this project: https://github.com/alittleteap0t/SojeeChatClient
15. Open up the project and edit config.json so that the IP address at the end of the file matches your host IP address.
16. Then, press F5 to run Sojee.


