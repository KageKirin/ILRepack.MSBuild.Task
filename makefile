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
	dotnet build -bl -v d

p pack:
	dotnet pack -bl -v d

t test:
	dotnet test -bl -v d
	dotnet run --project XmlFormat.Tool -- --help
	dotnet run --project XmlFormat.Tool -- --version
	dotnet run --project XmlFormat.Tool -- --inline test.xml
	.artifacts/bin/XmlFormat.Tool/debug/XmlFormat.Tool --help
	.artifacts/bin/XmlFormat.Tool/debug/XmlFormat.Tool --version
	.artifacts/bin/XmlFormat.Tool/debug/XmlFormat.Tool --inline test.xml

publish:
	dotnet push

results:
	ls -alG .artifacts/bin/XmlFormat.MsBuild.Task/debug
	ls -alG .artifacts/bin/XmlFormat.Tool/debug
