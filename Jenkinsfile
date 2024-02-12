pipeline{
    agent any

    stages {
        stage('Build') {
            steps {
                echo 'Building..'
                 script {
                    sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
                    sh "sudo dotnet build -c Debug"
                }
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'
                   
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'sudo dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/'
                
                sh 'sudo chmod +x restart-test.sh'
                sh 'bash restart-test.sh'
            }
        }
    }
    post {
        always {
            discordSend description: "Builder for plugin AmiaReforged finished.", footer: "Build results for AmiaReforged", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: "https://discord.com/api/webhooks/957814431704842270/2A6zZ4x7fsWULXnfrLLyRvgqexcnAvreXr6fbym8IoHdAHGpEoQgXjLn1XKry75uN_Zg"
            sh "sudo chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
        }
        success {
            echo 'Build success'
        }
    }
}