# Bulk CloudWatch Alarms For EC2 Instances
Create CloudWatch alarms in bulk for a given list of EC2-instances via AWS SDK

I needed to create the same set of CloudWatch alarms for a list of EC2 instances (~100 instances), and didn't want to do it manually. So I wrote this script to do it for me.
It will check if an alarm already exists for a given instance, and if it does, it will skip it. If it doesn't, it will create it.
It's a rare use-case, but preserving here in case I need to do this again in future or if someone else needs to do the same thing.
