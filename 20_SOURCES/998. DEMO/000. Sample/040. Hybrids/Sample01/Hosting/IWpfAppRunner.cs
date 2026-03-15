namespace Sample01.Hosting;

public interface IWpfAppRunner
{
	void Run();

	void Shutdown(int exitCode = 0);
}
