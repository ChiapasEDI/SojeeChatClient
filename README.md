# SojeeChatClient

This is Sojee, Chiapas EDI Technologies, Inc. AI front-end chatbot.  It relies on the text-generation-webui as an AI inference engine in order to answer questions.  For a video that explains the business requirements behind Sojee and then goes step by step through installing it and customizing it, visit https://youtu.be/pjNjdcRf2TE

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

To customize Sojee with your own corpus, you'll need to follow these steps:
1. Categorize your corpus into a number of topics.  Each topic can not exceed somewhere around 7900-8000 tokens in length - about 15K of code or 30K of text.  You can use paste your text into a text-generation-webui interface window to get a token count of your topic.
2. Edit the Index.razor file so that it introduces your particular topic.
3. Edit the config.json file so that it references the IP of the text-generation-webui backend AI inference engine, as referenced above, as well as specifying the topics.  Leave the initial topic in place.
4. Edit prompt_initial.txt so that it has a lot of general information about your corpus enough to categorize the question into one of the topics you have.  The first and last sentences should be customized a bit for your topics, following the prompt_initial.txt as an example.
5. You'll want a prompt_general.txt (or something like it, similar to prompt_business.txt in Sojee) as a 'catch all' topic for questions that don't fall into a particular topic.  You can copy/paste the body from prompt_initial.txt for this.
6. Make all the rest of your topics in the form of prompt_<topic>.txt, where <topic> is one of the items in the config.json file.
7. It may be necessary that you want to override the default inference hyperparameters for your particular topic in order to improve the results.  See the ChatBotParameters.cs for examples of what they are.  I use seed:1 so that the same question will generate the same answer, but this may not be desirable for some cases.
8. Also make sure that each of your prompt text files are: 1) Not saved in Unicode and 2) Are set to "Copy Always" in the project
9. After these changes, running the project should let you query your topic text files.  The default Publish profile in the project is meant to save a Linux executable - copy the publish folder contents to a Linux VM, chmod +x to the executable, and run it with ./SojeeChat --urls (URL to serve the Sojee Chatbot).

For questions, reach out to Richard @ richard@chiapas-edi.org.  
