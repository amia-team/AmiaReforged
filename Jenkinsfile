pipeline{
    agent any

    stages {
        stage('Deploy Test') {
            when {
                    expression {
                        return params.DeployTest == true
                    }
            }
            steps {
                echo 'Deploying....'
				sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'sudo chmod +x stop-test.sh'
				sh 'bash stop-test.sh'

                sh 'sudo dotnet publish AmiaReforged.Core --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/'
                sh 'sudo dotnet publish AmiaReforged.System --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'sudo dotnet publish AmiaReforged.Races --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/'
                sh 'sudo dotnet publish AmiaReforged.DMS --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.DMS/'
                sh 'sudo dotnet publish AmiaReforged.PwEngine --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.PwEngine/'

                script {
                    if (params.resources_dest_test?.trim()) {
                        sh "sudo rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_test}/"
                    } else {
                        echo 'WARNING: resources_dest_test parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'sudo chmod +x start-test.sh'
				sh 'bash start-test.sh'
            }
        }
		stage('Deploy Live') {
            when {
                    expression {
                        return params.DeployLive == true
                    }
            }
            steps {
                echo 'Deploying....'
				sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'sudo chmod +x stop-live.sh'
				sh 'bash stop-live.sh'

                sh 'sudo dotnet publish AmiaReforged.Core --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Core/'
                sh 'sudo dotnet publish AmiaReforged.System --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.System/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'sudo dotnet publish AmiaReforged.Races --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Races/'
                sh 'sudo dotnet publish AmiaReforged.DMS --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.DMS/'
                sh 'sudo dotnet publish AmiaReforged.PwEngine --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.PwEngine/'

                script {
                    if (params.resources_dest_prod?.trim()) {
                        sh "sudo rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_prod}/"
                    } else {
                        echo 'WARNING: resources_dest_prod parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'sudo chmod +x start-live.sh'
				sh 'bash start-live.sh'
            }
        }
    }
    post {
        always {
            discordSend description: "Builder for plugin AmiaReforged finished.", footer: "Build results for AmiaReforged", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: params.webhookURL
        }
        success {
            echo 'Build success'
        }
    }
}
