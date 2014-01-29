# vim: tabstop=4:softtabstop=4:shiftwidth=4:noexpandtab
require 'fuburake'

@solution = FubuRake::Solution.new do |sln|
	sln.compile = {
		:solutionfile => 'src/ripple.sln'
	}

	sln.assembly_info = {
		:product_name => "ripple",
		:copyright => 'Copyright 2008-2013 Jeremy D. Miller, Josh Arnold, et al. All rights reserved.'
	}

	sln.ripple_enabled = true
	sln.fubudocs_enabled = true
	
	sln.ci_steps = [:archive_gem]
end

desc "Replaces the existing installed gem with the new version for local testing"
task :local_gem => [:create_gem] do
	puts 'THIS TASK DOES NOT UNINSTALL OLD VERSIONS!'
	#sh 'gem uninstall ripple-cli -ax'
	Dir.chdir 'pkg' do
	    sh 'gem install ripple-cli'
    end
end

desc "Moves the gem to the archive folder"
task :archive_gem => [:create_gem] do
	copyOutputFiles "pkg", "*.gem", "artifacts"
end


desc "Creates the gem for fubu.exe"
task :create_gem => [:compile] do
	require "rubygems/package"
	include FileUtils
	cleanDirectory 'bin'	
	cleanDirectory 'pkg'
	
	Dir.mkdir 'bin' unless Dir.exists?('bin')
	Dir.mkdir 'pkg' unless Dir.exists?('pkg')
	
	copyOutputFiles "src/ripple/bin/#{@solution.compilemode}", '*.dll', 'bin'
	copyOutputFiles "src/ripple/bin/#{@solution.compilemode}", 'ripple.exe', 'bin'
	copyOutputFiles "src/ripple/bin/#{@solution.compilemode}", 'run-git.cmd', 'bin'
	
	FileUtils.copy 'ripple', 'bin'


	spec = Gem::Specification.new do |s|
		s.name          = "ripple-cli"
		s.version       = @solution.options[:build_number]
		s.authors       = ["Jeremy Miller", "Josh Arnold", "Ryan Rauh", "Matthew Smith"]
		s.email         = ["fubumvc-devel@googlegroups.com"]
		s.description   = "Ripple is a tool that wraps Nuget with workflow more conducive to upstream/downstream development across code repositories"
		s.summary       = "Improved dependency management with Nuget"
		s.homepage      = "http://fubu-project.org"
		s.license       = "Apache 2.0"

		s.files         = Dir.glob("bin/**/*").to_a
		s.executables   = ["ripple"]
		s.require_paths = ["lib"]

		s.add_development_dependency "bundler", "~> 1.3.5"
		s.add_development_dependency "rake"
	end  
	
    puts "ON THE FLY SPEC FILES"
    puts spec.files
    puts "=========="

    Gem::Package::build spec, true
	
	FileUtils.mv "ripple-cli-#{@solution.options[:build_number]}.gem", "pkg/ripple-cli-#{@solution.options[:build_number]}.gem"
	
end
