begin
  require 'bundler/setup'
  require 'fuburake'
rescue LoadError
  puts 'Bundler and all the gems need to be installed prior to running this rake script. Installing...'
  system("gem install bundler --source http://rubygems.org")
  sh 'bundle install'
  system("bundle exec rake", *ARGV)
  exit 0
end



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
	
	sln.ci_steps = ["gem:archive"]
end

BUILD_NUMBER = @solution.options[:build_number]

load File.expand_path('../ripple-cli/Rakefile', __FILE__)
