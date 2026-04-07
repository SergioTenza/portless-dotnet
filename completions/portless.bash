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
