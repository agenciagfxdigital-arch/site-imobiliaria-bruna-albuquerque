using System.Text;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Services;

namespace GFX.SiteBruna.Api.Comandos;

/// <summary>
/// <c>dotnet run --project src/Back/GFX.SiteBruna.Api -- criar-usuario email@dominio "Nome"</c>: cria um usuário do
/// painel (o primeiro, depois de aplicar as migrations). A senha é pedida no terminal, sem aparecer na tela e sem ir
/// para o histórico do shell.
/// </summary>
public static class CriarUsuario
{
    public const string Nome = "criar-usuario";

    public static async Task<int> Executar(IServiceProvider servicos, string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Uso: criar-usuario <email> \"<nome>\"");
            return 1;
        }
        var senha = LerSenha("Senha (mínimo 12 caracteres, com maiúscula, minúscula, número e símbolo): ");
        if (senha != LerSenha("Repita a senha: "))
        {
            Console.Error.WriteLine("As senhas não conferem.");
            return 1;
        }

        await using var escopo = servicos.CreateAsyncScope();
        var resposta = await escopo.ServiceProvider.GetRequiredService<UsuarioService>().Criar(new CriarUsuarioRequest(args[1], args[2], senha));
        if (!resposta.Sucesso)
        {
            Console.Error.WriteLine(string.Join(" ", resposta.Mensagens));
            return 1;
        }
        Console.WriteLine($"Usuário criado: {resposta.Dados!.Nome} <{resposta.Dados.Email}>");
        return 0;
    }

    private static string LerSenha(string pergunta)
    {
        Console.Write(pergunta);
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace) { if (senha.Length > 0) senha.Length--; }
            else if (!char.IsControl(tecla.KeyChar)) senha.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return senha.ToString();
    }
}
