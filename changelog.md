# Changelog

## [v0.9.2](https://github.com/devlooped/xunit.vsix/tree/v0.9.2) (2022-10-08)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.9.1...v0.9.2)

:twisted_rightwards_arrows: Merged:

- Emulate Roslyn's ensuring of extension manager loading [\#64](https://github.com/devlooped/xunit.vsix/pull/64) (@kzu)

## [v0.9.1](https://github.com/devlooped/xunit.vsix/tree/v0.9.1) (2022-09-28)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.9.0...v0.9.1)

:twisted_rightwards_arrows: Merged:

- Return failure when wait doesn't succeed [\#63](https://github.com/devlooped/xunit.vsix/pull/63) (@kzu)

## [v0.9.0](https://github.com/devlooped/xunit.vsix/tree/v0.9.0) (2022-09-07)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.8.0...v0.9.0)

:sparkles: Implemented enhancements:

- While debugging a test, timeouts should be disabled [\#57](https://github.com/devlooped/xunit.vsix/issues/57)
- Automatically attach debugger to VS if test is being debugged [\#55](https://github.com/devlooped/xunit.vsix/issues/55)

:twisted_rightwards_arrows: Merged:

- While debugging a test, timeouts should be disabled [\#58](https://github.com/devlooped/xunit.vsix/pull/58) (@kzu)
- Automatically attach debugger to VS if test is being debugged [\#56](https://github.com/devlooped/xunit.vsix/pull/56) (@kzu)

## [v0.8.0](https://github.com/devlooped/xunit.vsix/tree/v0.8.0) (2022-08-30)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.7.0...v0.8.0)

:hammer: Other:

- InvalidOperationException for Visual Studio 2017 [\#6](https://github.com/devlooped/xunit.vsix/issues/6)

:twisted_rightwards_arrows: Merged:

- Open C\# file, and only only on exp instance [\#53](https://github.com/devlooped/xunit.vsix/pull/53) (@kzu)
- Don't request DTE via COM since it can hang VS [\#50](https://github.com/devlooped/xunit.vsix/pull/50) (@kzu)
- Remove usage of COM/process based ISP wrapper [\#49](https://github.com/devlooped/xunit.vsix/pull/49) (@kzu)
- Further prevent hangs when running UI thread tests [\#46](https://github.com/devlooped/xunit.vsix/pull/46) (@kzu)
- Switch to VSSDK, use built-in primitives, address warnings [\#45](https://github.com/devlooped/xunit.vsix/pull/45) (@kzu)
- Kill the windows devenv process, since we're in bash [\#44](https://github.com/devlooped/xunit.vsix/pull/44) (@kzu)
- Kill devenv between test runs [\#43](https://github.com/devlooped/xunit.vsix/pull/43) (@kzu)

## [v0.7.0](https://github.com/devlooped/xunit.vsix/tree/v0.7.0) (2022-08-25)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.6.1...v0.7.0)

:hammer: Other:

- Improve UI thread handling by leveraging JoinableTaskContext [\#42](https://github.com/devlooped/xunit.vsix/issues/42)

:twisted_rightwards_arrows: Merged:

- Remove slow vs tool install and reset [\#41](https://github.com/devlooped/xunit.vsix/pull/41) (@kzu)

## [v0.6.1](https://github.com/devlooped/xunit.vsix/tree/v0.6.1) (2022-08-24)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.5.0...v0.6.1)

:bug: Fixed bugs:

- Speed up skipped tests execution [\#39](https://github.com/devlooped/xunit.vsix/issues/39)
- Allow disabling timeouts selectively without having to debug [\#38](https://github.com/devlooped/xunit.vsix/issues/38)
- Allow debugging remote VS process via CLI/envvars [\#37](https://github.com/devlooped/xunit.vsix/issues/37)
- After a new deployment of a VSIX, first test run randomly hangs VS [\#34](https://github.com/devlooped/xunit.vsix/issues/34)

:twisted_rightwards_arrows: Merged:

- Change back to previous retrying test runs [\#40](https://github.com/devlooped/xunit.vsix/pull/40) (@kzu)
- Do not force MEF component initialization [\#35](https://github.com/devlooped/xunit.vsix/pull/35) (@kzu)

## [v0.5.0](https://github.com/devlooped/xunit.vsix/tree/v0.5.0) (2022-08-18)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.5.0-beta...v0.5.0)

## [v0.5.0-beta](https://github.com/devlooped/xunit.vsix/tree/v0.5.0-beta) (2022-07-20)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.4.0...v0.5.0-beta)

## [v0.4.0](https://github.com/devlooped/xunit.vsix/tree/v0.4.0) (2022-05-13)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/v0.1.0...v0.4.0)

:hammer: Other:

- Only refresh VSIX registration if package is not registered [\#1](https://github.com/devlooped/xunit.vsix/issues/1)

:twisted_rightwards_arrows: Merged:

- Remove assembly-level attribute generation, rely on metadata [\#14](https://github.com/devlooped/xunit.vsix/pull/14) (@kzu)
- Minor updates [\#13](https://github.com/devlooped/xunit.vsix/pull/13) (@kzu)
- Re-enable all integration tests on CI [\#12](https://github.com/devlooped/xunit.vsix/pull/12) (@kzu)
- Shell fixes [\#5](https://github.com/devlooped/xunit.vsix/pull/5) (@kzu)
- Build fixes [\#4](https://github.com/devlooped/xunit.vsix/pull/4) (@kzu)
- Add 2017 support [\#3](https://github.com/devlooped/xunit.vsix/pull/3) (@kzu)
- removed dependency on VS 2010 assemblies and other fixes [\#2](https://github.com/devlooped/xunit.vsix/pull/2) (@victorgarciaaprea)

## [v0.1.0](https://github.com/devlooped/xunit.vsix/tree/v0.1.0) (2015-05-13)

[Full Changelog](https://github.com/devlooped/xunit.vsix/compare/2080c0763837b6efc648aebed0dcffc8b426af7a...v0.1.0)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
