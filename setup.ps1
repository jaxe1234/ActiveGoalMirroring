python setup_mac.py install
pip install git+https://github.com/pythonnet/pythonnet.git@71f6ed961731df2e28a6103236c8476b4c07837f
cd gym-cooking-fork
python setup.py install
cd ..
dotnet build --configuration Release