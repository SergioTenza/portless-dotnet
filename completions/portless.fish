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
