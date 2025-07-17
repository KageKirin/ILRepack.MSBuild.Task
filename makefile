GITHUB_TOKEN?=${GITHUB_TOKEN}
GITHUB_USER?=${GITHUB_USER}

f format:
	@fd . -td --max-depth 1 --search-path . | xargs -P 8 -I _ sh -c "echo _; cd _; fd '\.cs$$' -tf -X dotnet csharpier {}; fd '\.cs$$' -tf -X dos2unix -q -r {};"
	@fd csproj$$ -X dos2unix -q -r {}

r restore:
	dotnet restore -bl -v d

c clean:
	dotnet clean -bl -v d
	rm -rf .artifacts

b build:
	dotnet build -bl -v d ILRepack.Tool.MSBuild.Task
	dotnet build -bl -v d ILRepack.Lib.MSBuild.Task
	dotnet build -bl -v d

p pack:
	dotnet pack -bl -v d

t test:
	dotnet test -bl -v d

publish:
	dotnet push

results:
	ls -alG .artifacts/bin/ILRepack.Tool.MSBuild.Task/debug
	ls -alG .artifacts/bin/ILRepack.Lib.MSBuild.Task/debug
