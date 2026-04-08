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

    _arguments -C \
        '1:command:->command' \
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
