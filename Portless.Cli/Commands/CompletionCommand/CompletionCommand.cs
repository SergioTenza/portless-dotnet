using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.CompletionCommand;

public class CompletionCommand : AsyncCommand<CompletionSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, CompletionSettings settings, CancellationToken ct)
    {
        var shell = settings.Shell?.ToLowerInvariant() ?? "bash";

        var script = shell switch
        {
            "bash" => GetBashCompletion(),
            "zsh" => GetZshCompletion(),
            "fish" => GetFishCompletion(),
            "powershell" => GetPowerShellCompletion(),
            _ => null
        };

        if (script == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Unknown shell '{settings.Shell}'. Supported: bash, zsh, fish, powershell");
            return Task.FromResult(1);
        }

        Console.WriteLine(script);
        return Task.FromResult(0);
    }

    private static string GetBashCompletion() => """
# bash completion for portless
_portless() {
    local cur prev commands subcommands opts
    COMPREPLY=()
    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"
    commands="run list get alias hosts up tcp proxy cert completion"

    case "${COMP_CWORD}" in
        1)
            COMPREPLY=( $(compgen -W "${commands}" -- "${cur}") )
            ;;
        2)
            case "${COMP_WORDS[1]}" in
                proxy)
                    subcommands="start stop status"
                    COMPREPLY=( $(compgen -W "${subcommands}" -- "${cur}") )
                    ;;
                cert)
                    subcommands="install status uninstall check renew"
                    COMPREPLY=( $(compgen -W "${subcommands}" -- "${cur}") )
                    ;;
                hosts)
                    subcommands="sync clean"
                    COMPREPLY=( $(compgen -W "${subcommands}" -- "${cur}") )
                    ;;
                completion)
                    subcommands="bash zsh fish powershell"
                    COMPREPLY=( $(compgen -W "${subcommands}" -- "${cur}") )
                    ;;
                alias)
                    opts="--remove --host --protocol"
                    COMPREPLY=( $(compgen -W "${opts}" -- "${cur}") )
                    ;;
            esac
            ;;
        *)
            case "${prev}" in
                --path|-p|--backend|-b|--listen|-l|--file|-f)
                    # Argument completion - no suggestions
                    ;;
                *)
                    opts="--path --backend --listen --file --remove --host --protocol --help"
                    COMPREPLY=( $(compgen -W "${opts}" -- "${cur}") )
                    ;;
            esac
            ;;
    esac
}
complete -F _portless portless
""";

    private static string GetZshCompletion() => """
#compdef portless
# zsh completion for portless

_portless() {
    local -a commands subcommands
    commands=(
        'run:Run an app with a named URL'
        'list:List active routes'
        'get:Get the URL for a named service'
        'alias:Manage static route aliases'
        'hosts:Manage /etc/hosts entries'
        'up:Start routes from config file'
        'tcp:Manage TCP proxy routes'
        'proxy:Manage the reverse proxy'
        'cert:Manage TLS certificates'
        'completion:Generate shell completion'
    )

    _arguments -C \\
        '1:command:->command' \\
        '*::arg:->args'

    case $state in
        command)
            _describe 'command' commands
            ;;
        args)
            case ${words[1]} in
                proxy)
                    subcommands=('start:Start proxy' 'stop:Stop proxy' 'status:Check status')
                    _describe 'subcommand' subcommands
                    ;;
                cert)
                    subcommands=('install:Install cert' 'status:Trust status' 'uninstall:Remove cert' 'check:Check expiry' 'renew:Renew cert')
                    _describe 'subcommand' subcommands
                    ;;
                hosts)
                    subcommands=('sync:Sync hosts' 'clean:Clean hosts')
                    _describe 'subcommand' subcommands
                    ;;
                completion)
                    _values 'shell' bash zsh fish powershell
                    ;;
                run)
                    _arguments '--path[Path prefix]' '--backend[Backend URL]'
                    ;;
                alias)
                    _arguments '--remove[Remove alias]' '--host[Backend host]' '--protocol[Protocol]'
                    ;;
                up)
                    _arguments '--file[Config file path]'
                    ;;
                tcp)
                    _arguments '--listen[Listen port]' '--remove[Remove proxy]'
                    ;;
            esac
            ;;
    esac
}

_portless "$@"
""";

    private static string GetFishCompletion() => """
# fish completion for portless

# Disable file completions
complete -c portless -f

# Top-level commands
complete -c portless -n '__fish_use_subcommand' -a 'run' -d 'Run an app with a named URL'
complete -c portless -n '__fish_use_subcommand' -a 'list' -d 'List active routes'
complete -c portless -n '__fish_use_subcommand' -a 'get' -d 'Get URL for a service'
complete -c portless -n '__fish_use_subcommand' -a 'alias' -d 'Manage static aliases'
complete -c portless -n '__fish_use_subcommand' -a 'hosts' -d 'Manage /etc/hosts'
complete -c portless -n '__fish_use_subcommand' -a 'up' -d 'Start routes from config'
complete -c portless -n '__fish_use_subcommand' -a 'tcp' -d 'Manage TCP proxy'
complete -c portless -n '__fish_use_subcommand' -a 'proxy' -d 'Manage reverse proxy'
complete -c portless -n '__fish_use_subcommand' -a 'cert' -d 'Manage certificates'
complete -c portless -n '__fish_use_subcommand' -a 'completion' -d 'Shell completion'

# proxy subcommands
complete -c portless -n '__fish_seen_subcommand_from proxy' -a 'start' -d 'Start proxy'
complete -c portless -n '__fish_seen_subcommand_from proxy' -a 'stop' -d 'Stop proxy'
complete -c portless -n '__fish_seen_subcommand_from proxy' -a 'status' -d 'Proxy status'

# cert subcommands
complete -c portless -n '__fish_seen_subcommand_from cert' -a 'install' -d 'Install cert'
complete -c portless -n '__fish_seen_subcommand_from cert' -a 'status' -d 'Trust status'
complete -c portless -n '__fish_seen_subcommand_from cert' -a 'uninstall' -d 'Remove cert'
complete -c portless -n '__fish_seen_subcommand_from cert' -a 'check' -d 'Check expiry'
complete -c portless -n '__fish_seen_subcommand_from cert' -a 'renew' -d 'Renew cert'

# hosts subcommands
complete -c portless -n '__fish_seen_subcommand_from hosts' -a 'sync' -d 'Sync hosts'
complete -c portless -n '__fish_seen_subcommand_from hosts' -a 'clean' -d 'Clean hosts'

# completion shells
complete -c portless -n '__fish_seen_subcommand_from completion' -a 'bash'
complete -c portless -n '__fish_seen_subcommand_from completion' -a 'zsh'
complete -c portless -n '__fish_seen_subcommand_from completion' -a 'fish'
complete -c portless -n '__fish_seen_subcommand_from completion' -a 'powershell'

# Flags
complete -c portless -n '__fish_seen_subcommand_from run' -l path -d 'Path prefix' -r
complete -c portless -n '__fish_seen_subcommand_from run' -l backend -d 'Backend URL' -r
complete -c portless -n '__fish_seen_subcommand_from up' -l file -d 'Config file' -r
complete -c portless -n '__fish_seen_subcommand_from tcp' -l listen -d 'Listen port' -r
complete -c portless -n '__fish_seen_subcommand_from alias' -l remove -d 'Remove alias'
""";

    private static string GetPowerShellCompletion() => """
# PowerShell completion for portless
Register-ArgumentCompleter -Native -CommandName portless -ScriptBlock {
    param($commandName, $wordToComplete, $cursorPosition)
    
    $commands = @('run', 'list', 'get', 'alias', 'hosts', 'up', 'tcp', 'proxy', 'cert', 'completion')
    
    if ($wordToComplete -eq '' -or $wordToComplete -notmatch '-') {
        $commands | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}
""";
}
