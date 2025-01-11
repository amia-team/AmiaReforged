pipeline{
    agent any

    stages {
        stage('Build') {
            steps {
                echo 'Building..'
                 script {
                    sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
                    sh 'sudo chmod +x stop-test.sh'
                    sh 'bash stop-test.sh'
                }
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'
                   
                sh 'sudo dotnet publish AmiaReforged.Core --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/'
                sh 'sudo dotnet publish AmiaReforged.System --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'sudo dotnet publish AmiaReforged.Races --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/'
                sh 'sudo dotnet publish AmiaReforged.DMS --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.DMS/'
                sh 'sudo dotnet publish AmiaReforged.PwEngine --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.PwEngine/'
                
            }
        }
    }
    post {
        always {
            discordSend description: "Builder for plugin AmiaReforged finished.", footer: "Build results for AmiaReforged", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: params.webhookURL
            sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
            sh 'sudo chmod +x start-test.sh'
            sh 'bash start-test.sh'
        }
        success {
            echo 'Build success'
        }
    }
}